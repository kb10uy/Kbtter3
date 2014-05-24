using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
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
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        public static readonly string ConfigurationFolderName = "config";
        public readonly string ConsumerTokenFileName = ConfigurationFolderName + "/consumer.json";
        public readonly string AccessTokenFileName = ConfigurationFolderName + "/users.json";
        public readonly string SystemDataFileName = ConfigurationFolderName + "/system.json";

        public readonly string ConsumerDefaultKey = "5bI3XiTNEMHiamjMV5Acnqkex";
        public readonly string ConsumerDefaultSecret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN";

        public Tokens Token { get; set; }

        public IDisposable Stream { get; protected set; }

        public List<Status> Cache { get; set; }

        public List<AccessToken> AccessTokens { get; set; }

        public ConsumerToken ConsumerToken { get; private set; }

        public event Action<StatusMessage> OnStatus;
        public event Action<EventMessage> OnEvent;

        public StatusMessage LatestStatus { get; set; }
        public EventMessage LatestEvent { get; set; }

        public OAuth.OAuthSession OAuthSession { get; set; }

        public Queue<Status> ShowingStatuses { get; private set; }

        public User AuthenticatedUser { get; set; }

        public static readonly string CacheDatabaseFileNameSuffix = "-cache.db";
        public static readonly string CacheDatabaseFolderName = "cache";

        public SQLiteConnection CacheDatabaseConnection { get; set; }
        public DataContext CacheContext { get; set; }

        public Kbtter3SystemData SystemData { get; set; }

        private SQLiteCommand AddFavoriteCacheCommand { get; set; }
        private SQLiteCommand AddRetweetCacheCommand { get; set; }
        private SQLiteCommand RemoveFavoriteCacheCommand { get; set; }
        private SQLiteCommand RemoveRetweetCacheCommand { get; set; }
        private SQLiteCommand IsFavoritedCommand { get; set; }
        private SQLiteCommand IsRetweetedCommand { get; set; }

        private Kbtter()
        {

        }

        ~Kbtter()
        {
            StopStreaming();
            SystemData.SaveJson(SystemDataFileName);
            if (CacheDatabaseConnection != null) CacheDatabaseConnection.Dispose();
        }

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

        public void Initialize()
        {
            ShowingStatuses = new Queue<Status>();

            AccessTokens = new List<AccessToken>();
            if (!Directory.Exists(ConfigurationFolderName)) Directory.CreateDirectory(ConfigurationFolderName);
            if (!Directory.Exists(CacheDatabaseFolderName)) Directory.CreateDirectory(CacheDatabaseFolderName);
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


        #region SQLiteキャッシュ関係
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
            AuthenticatedUser = await Token.Account.VerifyCredentialsAsync(include_entities => true);

            var sb = new SQLiteConnectionStringBuilder()
            {
                DataSource = CacheDatabaseFolderName + "/" + AccessTokens[ai].ScreenName + CacheDatabaseFileNameSuffix,
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

        public Tokens AuthorizeToken(string pin)
        {
            var t = OAuthSession.GetTokens(pin);
            return t;
        }
        #endregion

        #region streaming
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
            Stream = ob.Connect();

            OnStatus += NotifyStatusUpdate;
            OnEvent += NotifyEventUpdate;


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

        private Task CacheEvents(EventMessage msg)
        {
            return Task.Run(() =>
            {
                if (AuthenticatedUser == null) return;
                switch (msg.Event)
                {
                    case EventCode.Favorite:
                        if (msg.Source.Id != AuthenticatedUser.Id) return;
                        AddFavoriteCache(msg.TargetStatus);
                        CacheContext.SubmitChanges();
                        break;
                    case EventCode.Unfavorite:
                        if (msg.Source != AuthenticatedUser) return;
                        RemoveFavoriteCache(msg.TargetStatus);
                        CacheContext.SubmitChanges();
                        break;

                }
            });
        }

        private Task CacheStatuses(StatusMessage msg)
        {
            return Task.Run(() =>
            {
                Cache.Add(msg.Status);
                switch (msg.Type)
                {
                    case MessageType.Create:
                        var cst = msg.Status;
                        if (cst.RetweetedStatus != null && cst.User.Id == AuthenticatedUser.Id)
                        {
                            AddRetweetCache(cst);
                            CacheContext.SubmitChanges();
                        }
                        break;
                    case MessageType.Delete:
                        var dst = msg.Status;
                        if (dst.RetweetedStatus != null && dst.User.Id == AuthenticatedUser.Id)
                        {
                            RemoveRetweetCache(dst);
                            CacheContext.SubmitChanges();
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

    public static class Kbtter3Extension
    {
        public static void SaveJson<T>(this T obj, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(obj));
        }
    }

    public class AccessToken
    {
        public string ScreenName { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
    }

    public class ConsumerToken
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }

    public class Kbtter3SystemData
    {
        public long LastFavoritedStatusId { get; set; }
        public long LastRetweetedStatusId { get; set; }

        public Kbtter3SystemData()
        {
            LastFavoritedStatusId = 0;
            LastRetweetedStatusId = 0;
        }
    }
}
