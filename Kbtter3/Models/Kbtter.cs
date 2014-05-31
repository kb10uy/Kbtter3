using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using CoreTweet;
using CoreTweet.Core;
using CoreTweet.Rest;
using CoreTweet.Streaming;
using CoreTweet.Streaming.Reactive;

using System.Reactive;
using System.Reactive.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using System.Data.SQLite;
using System.Data.SQLite.Linq;
using System.Data.Linq;
using System.Data;

using Kbtter3.Models.Caching;
using Livet;

namespace Kbtter3.Models
{
    public class Kbtter : NotificationObject
    {
        internal readonly string ConsumerTokenFileName = App.ConfigurationFolderName + "/consumer.json";
        internal readonly string AccessTokenFileName = App.ConfigurationFolderName + "/users.json";
        internal readonly string SystemDataFileName = App.ConfigurationFolderName + "/system.json";

        internal readonly string ConsumerDefaultKey = "5bI3XiTNEMHiamjMV5Acnqkex";
        internal readonly string ConsumerDefaultSecret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN";

        public Tokens Token { get; set; }

        internal IDisposable Stream { get; set; }

        public List<Status> Cache { get; set; }

        public List<AccessToken> AccessTokens { get; set; }

        public ConsumerToken ConsumerToken { get; private set; }

        public event Action<StatusMessage> OnStatus;
        public event Action<EventMessage> OnEvent;
        public event Action<IdMessage> OnIdEvent;

        internal StatusMessage LatestStatus { get; set; }
        internal EventMessage LatestEvent { get; set; }

        internal OAuth.OAuthSession OAuthSession { get; set; }

        internal Queue<Status> ShowingStatuses { get; private set; }

        public User AuthenticatedUser { get; set; }

        internal static readonly string CacheDatabaseFileNameSuffix = "-cache.db";
        internal static readonly string CacheUserImageFileNameSuffix = "-icon.png";
        internal static readonly string CacheUserBackgroundImageFileNameSuffix = "-background.png";
        internal static readonly string CacheUserProfileFileNameSuffix = "-profile.json";
        internal static readonly string CacheFolderName = "cache";

        private SQLiteConnection CacheDatabaseConnection { get; set; }
        internal DataContext CacheContext { get; set; }

        internal Kbtter3SystemData SystemData { get; set; }

        private SQLiteCommand AddFavoriteCacheCommand { get; set; }
        private SQLiteCommand AddRetweetCacheCommand { get; set; }
        private SQLiteCommand RemoveFavoriteCacheCommand { get; set; }
        private SQLiteCommand RemoveRetweetCacheCommand { get; set; }
        private SQLiteCommand IsFavoritedCommand { get; set; }
        private SQLiteCommand IsRetweetedCommand { get; set; }
        private SQLiteCommand IsMyRetweetCommand { get; set; }

        #region コンストラクタ・デストラクタ
        private Kbtter()
        {

        }

        ~Kbtter()
        {
            StopStreaming();
            SystemData.SaveJson(SystemDataFileName);
            if (CacheDatabaseConnection != null) CacheDatabaseConnection.Dispose();
        }
        #endregion

        #region シングルトン
        static Kbtter _instance;
        public static Kbtter Instance
        {
            get
            {
                if (_instance == null) _instance = new Kbtter();
                return _instance;
            }
        }
        #endregion

        internal void Initialize()
        {
            ShowingStatuses = new Queue<Status>();

            AccessTokens = new List<AccessToken>();

            if (!Directory.Exists(CacheFolderName)) Directory.CreateDirectory(CacheFolderName);

            if (!File.Exists(ConsumerTokenFileName))
            {
                var ct = new ConsumerToken { Key = ConsumerDefaultKey, Secret = ConsumerDefaultSecret };
                var json = JsonConvert.SerializeObject(ct);
                File.WriteAllText(ConsumerTokenFileName, json);
            }
            AccessTokens = LoadJson<List<AccessToken>>(AccessTokenFileName);

            var cjs = File.ReadAllText(ConsumerTokenFileName);
            ConsumerToken = JsonConvert.DeserializeObject<ConsumerToken>(cjs);
            OAuthSession = OAuth.Authorize(ConsumerToken.Key, ConsumerToken.Secret);

            SystemData = LoadJson<Kbtter3SystemData>(SystemDataFileName);

            RaisePropertyChanged("AccessTokenRequest");
        }


        #region キャッシュ関係
        private async void InitializeUserCache()
        {
            if (AuthenticatedUser != null)
            {
                var upc = new UserProfileCache
                {
                    Name = AuthenticatedUser.Name,
                    ScreenName = AuthenticatedUser.ScreenName,
                    Description = AuthenticatedUser.Description,
                    Location = AuthenticatedUser.Location,
                    Uri = AuthenticatedUser.Url.ToString(),
                    Statuses = AuthenticatedUser.StatusesCount,
                    Friends = AuthenticatedUser.FriendsCount,
                    Followers = AuthenticatedUser.FollowersCount,
                    Favorites = AuthenticatedUser.FavouritesCount,
                };
                upc.SaveJson(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserProfileFileNameSuffix);
                using (var wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(
                        AuthenticatedUser.ProfileImageUrlHttps,
                        CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserImageFileNameSuffix);
                    await wc.DownloadFileTaskAsync(
                        AuthenticatedUser.ProfileBackgroundImageUrlHttps,
                        CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserBackgroundImageFileNameSuffix);
                }
            }
            else
            {
                if (!File.Exists(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserProfileFileNameSuffix)) return;
                var upc = LoadJson<UserProfileCache>(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserProfileFileNameSuffix);
                AuthenticatedUser.Name = upc.Name;
                AuthenticatedUser.ScreenName = upc.ScreenName;
                AuthenticatedUser.Description = upc.Description;
                AuthenticatedUser.Location = upc.Location;
                AuthenticatedUser.Url = new Uri(upc.Uri);
                AuthenticatedUser.StatusesCount = upc.Statuses;
                AuthenticatedUser.FriendsCount = upc.Friends;
                AuthenticatedUser.FollowersCount = upc.Followers;
                AuthenticatedUser.FavouritesCount = upc.Favorites;
                if (File.Exists(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserImageFileNameSuffix))
                {
                    AuthenticatedUser.ProfileImageUrlHttps = new Uri(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserImageFileNameSuffix, UriKind.Relative);
                }
                if (File.Exists(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserBackgroundImageFileNameSuffix))
                {
                    AuthenticatedUser.ProfileImageUrlHttps = new Uri(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserBackgroundImageFileNameSuffix, UriKind.Relative);
                }
            }
        }

        private void InitializeCacheDatabase(int ai)
        {

            var sb = new SQLiteConnectionStringBuilder()
            {
                DataSource = CacheFolderName + "/" + AccessTokens[ai].ScreenName + CacheDatabaseFileNameSuffix,
                Version = 3,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Memory
            };
            CacheDatabaseConnection = new SQLiteConnection(sb.ToString());
            CacheDatabaseConnection.Open();
            CacheContext = new DataContext(CacheDatabaseConnection);
            CreateTables();
            CacheUnknown();
        }

        private void CreateTables()
        {
            var c = CacheDatabaseConnection.CreateCommand();

            //TODO : よりスタイリッシュな方法の探索

            CacheContext.ExecuteCommand("CREATE TABLE IF NOT EXISTS Favorites(ID UNIQUE,DATE,NAME);");
            CacheContext.ExecuteCommand("CREATE TABLE IF NOT EXISTS Retweets(ID UNIQUE,ORIGINALID,DATE,NAME);");
            CacheContext.ExecuteCommand("CREATE TABLE IF NOT EXISTS Bookmarks(ID UNIQUE,DATE,SCREENNAME,NAME,STATUS);");

            AddFavoriteCacheCommand = CacheDatabaseConnection.CreateCommand();
            AddFavoriteCacheCommand.CommandText = "INSERT OR IGNORE INTO Favorites(ID,DATE,NAME) VALUES(@Id,@Date,@Name)";
            AddFavoriteCacheCommand.Parameters.Add("Id", DbType.Int64);
            AddFavoriteCacheCommand.Parameters.Add("Date", DbType.DateTime);
            AddFavoriteCacheCommand.Parameters.Add("Name", DbType.String);

            AddRetweetCacheCommand = CacheDatabaseConnection.CreateCommand();
            AddRetweetCacheCommand.CommandText = "INSERT OR IGNORE INTO Retweets(ID,ORIGINALID,DATE,NAME) VALUES(@Id,@OriginalId,@Date,@Name)";
            AddRetweetCacheCommand.Parameters.Add("Id", DbType.Int64);
            AddRetweetCacheCommand.Parameters.Add("OriginalId", DbType.Int64);
            AddRetweetCacheCommand.Parameters.Add("Date", DbType.DateTime);
            AddRetweetCacheCommand.Parameters.Add("Name", DbType.String);

            RemoveFavoriteCacheCommand = CacheDatabaseConnection.CreateCommand();
            RemoveFavoriteCacheCommand.CommandText = "DELETE FROM Favorites WHERE ID=@Id";
            RemoveFavoriteCacheCommand.Parameters.Add("Id", DbType.Int64);

            RemoveRetweetCacheCommand = CacheDatabaseConnection.CreateCommand();
            RemoveRetweetCacheCommand.CommandText = "DELETE FROM Retweets WHERE ID=@Id";
            RemoveRetweetCacheCommand.Parameters.Add("Id", DbType.Int64);

            IsFavoritedCommand = CacheDatabaseConnection.CreateCommand();
            IsFavoritedCommand.CommandText = "SELECT * FROM Favorites WHERE ID=@Id";
            IsFavoritedCommand.Parameters.Add("Id", DbType.Int64);

            IsRetweetedCommand = CacheDatabaseConnection.CreateCommand();
            IsRetweetedCommand.CommandText = "SELECT * FROM Retweets WHERE ORIGINALID=@OriginalId";
            IsRetweetedCommand.Parameters.Add("OriginalId", DbType.Int64);

            IsMyRetweetCommand = CacheDatabaseConnection.CreateCommand();
            IsMyRetweetCommand.CommandText = "SELECT * FROM Retweets WHERE ID=@Id";
            IsMyRetweetCommand.Parameters.Add("Id", DbType.Int64);
        }

        private async void CacheUnknown()
        {
            var ls = await Token.Favorites.ListAsync(count => 200);
            foreach (var i in ls) AddFavoriteCache(i);

            //foreach (var i in rs) AddRetweetCache(i);

            CacheContext.SubmitChanges();
        }

        public void AddFavoriteCache(Status st)
        {
            AddFavoriteCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.Parameters["Date"].Value = st.CreatedAt.DateTime;
            AddFavoriteCacheCommand.Parameters["Name"].Value = st.User.ScreenName;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        public void AddRetweetCache(Status st)
        {
            AddRetweetCacheCommand.Parameters["Id"].Value = st.Id;
            AddRetweetCacheCommand.Parameters["OriginalId"].Value = st.RetweetedStatus.Id;
            AddRetweetCacheCommand.Parameters["Date"].Value = st.CreatedAt.DateTime;
            AddRetweetCacheCommand.Parameters["Name"].Value = st.User.ScreenName;
            AddRetweetCacheCommand.ExecuteNonQuery();
        }

        public void RemoveFavoriteCache(Status st)
        {
            RemoveFavoriteCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        public void RemoveRetweetCache(Status st)
        {
            RemoveRetweetCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        public void RemoveRetweetCache(long stid)
        {
            RemoveRetweetCacheCommand.Parameters["Id"].Value = stid;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        public bool IsFavoritedInCache(Status st)
        {
            IsFavoritedCommand.Parameters["Id"].Value = st.Id;
            using (var dr = IsFavoritedCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        public bool IsRetweetedInCache(Status st)
        {
            IsRetweetedCommand.Parameters["OriginalId"].Value = st.RetweetedStatus.Id;
            using (var dr = IsRetweetedCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        public bool IsmyRetweetInCache(long stid)
        {
            IsMyRetweetCommand.Parameters["Id"].Value = stid;
            using (var dr = IsMyRetweetCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        #endregion

        #region 認証
        public async void AuthenticateWith(int ai)
        {
            Token = Tokens.Create(
                ConsumerToken.Key,
                ConsumerToken.Secret,
                AccessTokens[ai].Token,
                AccessTokens[ai].TokenSecret
                );
            Cache = new List<Status>();
            InitializeCacheDatabase(ai);

            AuthenticatedUser = await Token.Account.VerifyCredentialsAsync(include_entities => true);
            InitializeUserCache();
            RaisePropertyChanged(() => AuthenticatedUser);
        }


        public Tokens AuthorizeToken(string pin)
        {
            var t = OAuthSession.GetTokens(pin);
            return t;
        }
        #endregion

        #region Streaming
        public void StartStreaming()
        {
            var ob = Token.Streaming.StartObservableStream(StreamingType.User, new StreamingParameters(include_entities => "true", include_followings_activity => "true"))
                .Publish();
            ob.OfType<StatusMessage>().Subscribe((p) =>
            {
                if (OnStatus != null) OnStatus(p);
            });
            ob.OfType<EventMessage>().Subscribe((p) =>
            {
                if (OnEvent != null) OnEvent(p);
            });
            ob.OfType<IdMessage>().Subscribe((p) =>
            {
                if (OnIdEvent != null) OnIdEvent(p);
            });
            ob.OfType<DisconnectMessage>().Subscribe(p =>
            {
                App.Current.Shutdown();
            });
            Stream = ob.Connect();

            OnStatus += NotifyStatusUpdate;
            OnEvent += NotifyEventUpdate;
            OnIdEvent += NotifyIdEventUpdate;

        }

        private async void NotifyStatusUpdate(StatusMessage msg)
        {
            if (msg.Type != MessageType.Create) return;
            LatestStatus = msg;
            ShowingStatuses.Enqueue(msg.Status);
            await CacheStatuses(msg);
            RaisePropertyChanged("Status");
        }

        private async void NotifyEventUpdate(EventMessage msg)
        {
            LatestEvent = msg;
            await CacheEvents(msg);
            RaisePropertyChanged("Event");
        }

        private async void NotifyIdEventUpdate(IdMessage msg)
        {
            await CacheIdEvents(msg);
            RaisePropertyChanged("IdEvent");
        }

        private Task CacheEvents(EventMessage msg)
        {
            return Task.Run(() =>
            {
                if (AuthenticatedUser == null) return;
                switch (msg.Event)
                {

                    case EventCode.Favorite:
                        if (msg.Source.Id == AuthenticatedUser.Id)
                        {
                            AddFavoriteCache(msg.TargetStatus);
                            CacheContext.SubmitChanges();
                            AuthenticatedUser = msg.Source;
                            //AuthenticatedUser.FavouritesCount++;
                            RaisePropertyChanged(() => AuthenticatedUser);
                            break;
                        }
                        else
                        {

                        }
                        break;
                    case EventCode.Unfavorite:
                        if (msg.Source.Id == AuthenticatedUser.Id)
                        {
                            RemoveFavoriteCache(msg.TargetStatus);
                            CacheContext.SubmitChanges();
                            AuthenticatedUser = msg.Source;
                            //AuthenticatedUser.FavouritesCount--;
                            RaisePropertyChanged(() => AuthenticatedUser);
                            break;
                        }
                        else
                        {

                        }
                        break;
                }
            });
        }

        private Task CacheStatuses(StatusMessage msg)
        {
            return Task.Run(() =>
            {
                if (AuthenticatedUser == null) return;
                var mst = msg.Status;
                switch (msg.Type)
                {
                    case MessageType.Create:
                        Cache.Add(msg.Status);
                        if (msg.Status.User.Id == AuthenticatedUser.Id)
                        {
                            AuthenticatedUser = msg.Status.User;
                            //AuthenticatedUser.StatusesCount++;
                            RaisePropertyChanged(() => AuthenticatedUser);
                            if (mst.RetweetedStatus != null)
                            {
                                AddRetweetCache(mst);
                                CacheContext.SubmitChanges();
                            }
                        }
                        break;

                }
            });
        }

        public Task CacheIdEvents(IdMessage msg)
        {
            return Task.Run(() =>
            {
                if (AuthenticatedUser == null) return;
                switch (msg.Type)
                {
                    case MessageType.DeleteStatus:
                        if (msg.UserId == AuthenticatedUser.Id)
                        {
                            AuthenticatedUser.StatusesCount--;
                            RaisePropertyChanged(() => AuthenticatedUser);
                            if (IsmyRetweetInCache(msg.UpToStatusId ?? 0))
                            {
                                RemoveRetweetCache(msg.UpToStatusId ?? 0);
                                CacheContext.SubmitChanges();
                            }
                        }
                        break;
                }
            });
        }

        public void StopStreaming()
        {
            if (Stream != null) Stream.Dispose();
        }

        #endregion

        #region コンフィグ用メソッド

        public void AddToken(Tokens t)
        {
            AccessTokens.Add(new AccessToken { ScreenName = t.ScreenName, Token = t.AccessToken, TokenSecret = t.AccessTokenSecret });
            AccessTokens.SaveJson(AccessTokenFileName);
        }

        public T LoadJson<T>(string filename)
            where T : new()
        {
            if (!File.Exists(filename))
            {
                var o = new T();
                File.WriteAllText(filename, JsonConvert.SerializeObject(o));
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
        }

        public T LoadJson<T>(string filename, T def)
        {
            if (!File.Exists(filename))
            {
                File.WriteAllText(filename, JsonConvert.SerializeObject(def));
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
        }

        #endregion
    }

    #region json保存用クラスとか

    internal static class Kbtter3Extension
    {
        public static void SaveJson<T>(this T obj, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(obj));
        }
    }

    /// <summary>
    /// TwitterにOAuthでログインする際に必要なAccessTokenを
    /// 定義します。
    /// </summary>
    public class AccessToken
    {
        public string ScreenName { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
    }

    /// <summary>
    /// TwitterにOAuthでログインする際に必要なConsumerKey/Secretの組を
    /// 定義します。
    /// </summary>
    public class ConsumerToken
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }


    internal class Kbtter3SystemData
    {
        public long LastFavoritedStatusId { get; set; }
        public long LastRetweetedStatusId { get; set; }

        public Kbtter3SystemData()
        {
            LastFavoritedStatusId = 0;
            LastRetweetedStatusId = 0;
        }
    }

    /// <summary>
    /// ユーザー情報のキャッシュを定義します。
    /// </summary>
    public class UserProfileCache
    {
        public string Name { get; set; }
        public string ScreenName { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Uri { get; set; }
        public int Statuses { get; set; }
        public int Friends { get; set; }
        public int Followers { get; set; }
        public int Favorites { get; set; }
    }

    #endregion
}
