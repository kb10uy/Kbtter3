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
using System.Reactive.Subjects;

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
    /// <summary>
    /// Kbtter3 Polystyreneのモデル層を定義します。
    /// このクラスは継承できません。
    /// </summary>
    public sealed class Kbtter : NotificationObject
    {

        /// <summary>
        /// CoreTweet Token
        /// </summary>
        public Tokens Token { get; set; }

        internal IDisposable StreamManager { get; set; }
        internal IConnectableObservable<StreamingMessage> Streaming { get; set; }

        /// <summary>
        /// 起動時以降のツイートのキャッシュ
        /// </summary>
        public List<Status> Cache { get; set; }

        /// <summary>
        /// ユーザーのキャッシュ
        /// </summary>
        public IDictionary<string, User> UserCache { get; set; }

        /// <summary>
        /// 現在の設定
        /// </summary>
        internal Kbtter3Setting Setting { get; set; }

        /// <summary>
        /// ツイート受信時のイベント
        /// </summary>
        public event Action<StatusMessage> OnStatus;

        /// <summary>
        /// イベント受信時のイベント
        /// </summary>
        public event Action<EventMessage> OnEvent;

        /// <summary>
        /// ダイレクトメッセージ受信時のイベント
        /// </summary>
        public event Action<DirectMessageMessage> OnDirectMessage;

        /// <summary>
        /// IdEvent受信時のイベント
        /// </summary>
        public event Action<IdMessage> OnIdEvent;

        internal StatusMessage LatestStatus { get; set; }
        internal EventMessage LatestEvent { get; set; }
        internal DirectMessageMessage LatestDirectMessage { get; set; }

        internal OAuth.OAuthSession OAuthSession { get; set; }

        internal Queue<Status> ShowingStatuses { get; private set; }

        /// <summary>
        /// 現在認証しているユーザー
        /// </summary>
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

        /// <summary>
        /// なし
        /// </summary>
        ~Kbtter()
        {
            StopStreaming();
            if (CacheDatabaseConnection != null) CacheDatabaseConnection.Dispose();
        }
        #endregion

        #region シングルトン
        static Kbtter _instance;

        /// <summary>
        /// Kbtterの唯一のインスタンスを取得します。
        /// </summary>
        public static Kbtter Instance
        {
            get
            {
                if (_instance == null) _instance = new Kbtter();
                return _instance;
            }
        }
        #endregion

        internal async void Initialize()
        {
            ShowingStatuses = new Queue<Status>();
            Setting = new Kbtter3Setting();

            if (!Directory.Exists(CacheFolderName)) Directory.CreateDirectory(CacheFolderName);
            Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName, Setting);
            OAuthSession = await OAuth.AuthorizeAsync(Setting.Consumer.Key, Setting.Consumer.Secret);
            RaisePropertyChanged("AccessTokenRequest");
            OnStatus += NotifyStatusUpdate;
            OnEvent += NotifyEventUpdate;
            OnIdEvent += NotifyIdEventUpdate;
            OnDirectMessage += NotifyDirectMessageUpdate;

        }


        #region キャッシュ関係
        private Task InitializeUserCache()
        {
            return Task.Run(async () =>
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
                    var upc = Kbtter3Extension.LoadJson<UserProfileCache>(CacheFolderName + "/" + AuthenticatedUser.ScreenName + CacheUserProfileFileNameSuffix);
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
            });
        }

        private Task InitializeCacheDatabase(int ai)
        {
            return Task.Run(() =>
            {
                var sb = new SQLiteConnectionStringBuilder()
                {
                    DataSource = CacheFolderName + "/" + Setting.AccessTokens[ai].ScreenName + CacheDatabaseFileNameSuffix,
                    Version = 3,
                    SyncMode = SynchronizationModes.Off,
                    JournalMode = SQLiteJournalModeEnum.Memory
                };
                CacheDatabaseConnection = new SQLiteConnection(sb.ToString());
                CacheDatabaseConnection.Open();
                CacheContext = new DataContext(CacheDatabaseConnection);
                CreateTables();
                CacheUnknown();
            });
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

        /// <summary>
        /// ツイートをお気に入りのキャッシュに追加します。
        /// </summary>
        /// <param name="st">ツイート</param>
        public void AddFavoriteCache(Status st)
        {
            AddFavoriteCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.Parameters["Date"].Value = st.CreatedAt.DateTime;
            AddFavoriteCacheCommand.Parameters["Name"].Value = st.User.ScreenName;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// ツイートをリツイートのキャッシュに追加します。
        /// </summary>
        /// <param name="st">ツイート。リツイートした元ツイートではなく、リツイート自体を指定しください。</param>
        public void AddRetweetCache(Status st)
        {
            AddRetweetCacheCommand.Parameters["Id"].Value = st.Id;
            AddRetweetCacheCommand.Parameters["OriginalId"].Value = st.RetweetedStatus.Id;
            AddRetweetCacheCommand.Parameters["Date"].Value = st.CreatedAt.DateTime;
            AddRetweetCacheCommand.Parameters["Name"].Value = st.User.ScreenName;
            AddRetweetCacheCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// ツイートをお気に入りのキャッシュから削除します。
        /// </summary>
        /// <param name="st">ツイート</param>
        public void RemoveFavoriteCache(Status st)
        {
            RemoveFavoriteCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// ツイートをリツイートのキャッシュから削除します。
        /// </summary>
        /// <param name="st">ツイート。リツイートした元ツイートではなく、リツイート自体を指定しください。</param>
        public void RemoveRetweetCache(Status st)
        {
            RemoveRetweetCacheCommand.Parameters["Id"].Value = st.Id;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// ツイートをリツイートのキャッシュに追加します。
        /// </summary>
        /// <param name="stid">リツイートのID。リツイートした元ツイートではなく、リツイート自体を指定しください。</param>
        public void RemoveRetweetCache(long stid)
        {
            RemoveRetweetCacheCommand.Parameters["Id"].Value = stid;
            AddFavoriteCacheCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// 指定したツイートがお気に入りとしてキャッシュされているか検証します。
        /// </summary>
        /// <param name="st">ツイート</param>
        /// <returns>存在した場合はtrue</returns>
        public bool IsFavoritedInCache(Status st)
        {
            IsFavoritedCommand.Parameters["Id"].Value = st.Id;
            using (var dr = IsFavoritedCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        /// <summary>
        /// 指定したリツイートの元ツイートを現在のキャッシュのユーザーがリツイートしているか検証します。
        /// </summary>
        /// <param name="st">ツイート。リツイートした元ツイートではなく、リツイート自体を指定しください。</param>
        /// <returns>存在した場合はtrue</returns>
        public bool IsRetweetedInCache(Status st)
        {
            IsRetweetedCommand.Parameters["OriginalId"].Value = st.RetweetedStatus.Id;
            using (var dr = IsRetweetedCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        /// <summary>
        /// 指定したリツイートが自分のものか検証します。
        /// </summary>
        /// <param name="stid">リツイートのID。リツイートした元ツイートではなく、リツイート自体を指定しください。</param>
        /// <returns>存在した場合はtrue</returns>
        public bool IsMyRetweetInCache(long stid)
        {
            IsMyRetweetCommand.Parameters["Id"].Value = stid;
            using (var dr = IsMyRetweetCommand.ExecuteReader())
            {
                return dr.Read();
            }
        }

        /// <summary>
        /// C#ﾋﾞｰﾑﾋﾞﾋﾞﾋﾞﾋﾞﾋﾞwwwwww
        /// </summary>
        /// <param name="beam">なまえ</param>
        public void FireBeam(string beam)
        {
            if (Token == null) return;
            if (!Setting.System.BeamCount.ContainsKey(beam)) Setting.System.BeamCount[beam] = 1;
            try
            {
                Token.Statuses.UpdateAsync(status => String.Format("{0}ﾋﾞｰﾑﾋﾞﾋﾞﾋﾞﾋﾞﾋﾞwwwwww({1}回目) #Kbtter3", beam, Setting.System.BeamCount[beam]));
            }
            catch
            {
            }
            Task.Run(() =>
            {
                var p = ++Setting.System.BeamCount[beam];
                Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
                Setting.System.BeamCount[beam] = p;
                Setting.SaveJson(App.ConfigurationFileName);
            });
        }

        /// <summary>
        /// Javaはクソ。
        /// </summary>
        /// <param name="beam">なまえ</param>
        public void Hate(string beam)
        {
            if (Token == null) return;
            if (!Setting.System.HateCount.ContainsKey(beam)) Setting.System.HateCount[beam] = 1;
            try
            {
                Token.Statuses.UpdateAsync(status => String.Format("{0}はクソ。({1}回目) #Kbtter3", beam, Setting.System.HateCount[beam]));
            }
            catch
            {
            }
            Task.Run(() =>
            {
                var p = ++Setting.System.HateCount[beam];
                Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
                Setting.System.HateCount[beam] = p;
                Setting.SaveJson(App.ConfigurationFileName);
            });
        }

        /// <summary>
        /// GO is GOD
        /// </summary>
        /// <param name="beam">なまえ</param>
        public void God(string beam)
        {
            if (Token == null) return;
            if (!Setting.System.GodCount.ContainsKey(beam)) Setting.System.GodCount[beam] = 1;
            try
            {
                Token.Statuses.UpdateAsync(status => String.Format("{0} is GOD({1}回目) #Kbtter3", beam, Setting.System.GodCount[beam]));
            }
            catch
            {
            }
            Task.Run(() =>
            {
                var p = ++Setting.System.GodCount[beam];
                Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
                Setting.System.GodCount[beam] = p;
                Setting.SaveJson(App.ConfigurationFileName);
            });

        }

        /// <summary>
        /// いまからおもしろいこといいます
        /// </summary>
        /// <param name="s">ダジャレ</param>
        public async void SayDajare(string s)
        {
            if (Token == null) return;
            try
            {
                await Token.Statuses.UpdateAsync(status => "いまからおもしろいこといいます");
                await Token.Statuses.UpdateAsync(status => s);
                await Token.Statuses.UpdateAsync(status => "ありがとうございました");
            }
            catch
            {
            }
        }

        /// <summary>
        /// ユーザーを取得してみる
        /// </summary>
        /// <param name="sn">SN</param>
        /// <returns>User</returns>
        public Task<User> GetUser(string sn)
        {
            return Task<User>.Run(async () =>
            {
                if (UserCache.ContainsKey(sn)) return UserCache[sn];
                var us = await Token.Users.ShowAsync(screen_name => sn);
                UserCache[sn] = us;
                return us;
            });
        }

        #endregion

        #region 認証
        /// <summary>
        /// 指定番号のAccessTokenを用いてログインします。
        /// </summary>
        /// <param name="ai">AccessTokenのインデックス</param>
        public async void AuthenticateWith(int ai)
        {
            Token = Tokens.Create(
                Setting.Consumer.Key,
                Setting.Consumer.Secret,
                Setting.AccessTokens[ai].Token,
                Setting.AccessTokens[ai].TokenSecret
                );
            Cache = new List<Status>();
            UserCache = new Dictionary<string, User>();
            AuthenticatedUser = await Token.Account.VerifyCredentialsAsync(include_entities => true);
            await InitializeCacheDatabase(ai);
            await InitializeUserCache();
            UserCache[AuthenticatedUser.ScreenName] = AuthenticatedUser;
            RaisePropertyChanged(() => AuthenticatedUser);

        }

        /// <summary>
        /// PINコードを用いて認証します。
        /// </summary>
        /// <param name="pin">PINコード</param>
        /// <returns>成功した場合はTokens</returns>
        public Tokens AuthorizeToken(string pin)
        {
            try
            {
                var t = OAuthSession.GetTokens(pin);
                return t;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Streaming
        /// <summary>
        /// Streamingを開始します。
        /// </summary>
        public void StartStreaming()
        {
            GetDirectMessages();
            Streaming = Token.Streaming.StartObservableStream(StreamingType.User, new StreamingParameters(include_entities => "true", include_followings_activity => "true"))
                .Publish();
            Streaming.OfType<StatusMessage>().Subscribe((p) =>
            {
                OnStatus(p);
            });
            Streaming.OfType<EventMessage>().Subscribe((p) =>
            {
                OnEvent(p);
            });
            Streaming.OfType<IdMessage>().Subscribe((p) =>
            {
                OnIdEvent(p);
            });
            Streaming.OfType<DirectMessageMessage>().Subscribe((p) =>
            {
                OnDirectMessage(p);
            });
            Streaming.OfType<DisconnectMessage>().Subscribe(p =>
            {
                App.Current.Shutdown();
            });
            Streaming.OfType<WarningMessage>().Subscribe(p =>
            {
                Console.WriteLine(p.Message);
            });

            StreamManager = Streaming.Connect();
        }

        /// <summary>
        /// Streamingを再接続します。
        /// </summary>
        public void RestartStreaming()
        {
            StopStreaming();
            Streaming = Token.Streaming.StartObservableStream(StreamingType.User, new StreamingParameters(include_entities => "true", include_followings_activity => "true"))
                .Publish();
            Streaming.OfType<StatusMessage>().Subscribe((p) =>
            {
                OnStatus(p);
            });
            Streaming.OfType<EventMessage>().Subscribe((p) =>
            {
                OnEvent(p);
            });
            Streaming.OfType<IdMessage>().Subscribe((p) =>
            {
                OnIdEvent(p);
            });
            Streaming.OfType<DirectMessageMessage>().Subscribe((p) =>
            {
                OnDirectMessage(p);
            });
            Streaming.OfType<DisconnectMessage>().Subscribe(p =>
            {
                App.Current.Shutdown();
            });
            Streaming.OfType<WarningMessage>().Subscribe(p =>
            {
                Console.WriteLine(p.Message);
            });

            StreamManager = Streaming.Connect();
        }

        private async void GetDirectMessages()
        {
            var r = (await Token.DirectMessages.ReceivedAsync(count => 200)).ToList();
            r.RemoveAll(p => p.Sender.Id == AuthenticatedUser.Id);
            var s = (await Token.DirectMessages.SentAsync(count => 200)).ToList();
            s.AddRange(r);
            s.Sort((x, y) => DateTime.Compare(x.CreatedAt.LocalDateTime, y.CreatedAt.LocalDateTime));
            foreach (var i in s)
            {
                OnDirectMessage(new DirectMessageMessage { DirectMessage = i });
            }
        }

        private void NotifyStatusUpdate(StatusMessage msg)
        {
            if (msg.Type != MessageType.Create) return;
            LatestStatus = msg;
            ShowingStatuses.Enqueue(msg.Status);
            CacheStatuses(msg);
            RaisePropertyChanged("Status");
        }

        private void NotifyEventUpdate(EventMessage msg)
        {
            LatestEvent = msg;
            CacheEvents(msg);
            RaisePropertyChanged("Event");
        }

        private void NotifyIdEventUpdate(IdMessage msg)
        {
            CacheIdEvents(msg);
            RaisePropertyChanged("IdEvent");
        }

        private void NotifyDirectMessageUpdate(DirectMessageMessage msg)
        {
            LatestDirectMessage = msg;
            CacheDirectMessage(msg);
            RaisePropertyChanged("DirectMessage");
        }

        private void CacheEvents(EventMessage msg)
        {
            Task.Run(() =>
            {
                UserCache[msg.Source.ScreenName] = msg.Source;
                UserCache[msg.Target.ScreenName] = msg.Target;
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

        private void CacheStatuses(StatusMessage msg)
        {
            Task.Run(() =>
            {

                if (AuthenticatedUser == null) return;
                var mst = msg.Status;
                UserCache[mst.User.ScreenName] = mst.User;
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

        private void CacheDirectMessage(DirectMessageMessage msg)
        {
            Task.Run(() =>
            {

            });
        }

        private void CacheIdEvents(IdMessage msg)
        {
            Task.Run(() =>
            {
                if (AuthenticatedUser == null) return;
                switch (msg.Type)
                {
                    case MessageType.DeleteStatus:
                        if (msg.UserId == AuthenticatedUser.Id)
                        {
                            AuthenticatedUser.StatusesCount--;
                            RaisePropertyChanged(() => AuthenticatedUser);
                            if (IsMyRetweetInCache(msg.UpToStatusId ?? 0))
                            {
                                RemoveRetweetCache(msg.UpToStatusId ?? 0);
                                CacheContext.SubmitChanges();
                            }
                        }
                        break;
                }
            });
        }

        /// <summary>
        /// Streamingを停止します。
        /// </summary>
        public void StopStreaming()
        {
            if (StreamManager != null)
            {
                StreamManager.Dispose();
                StreamManager = null;
            }
        }

        #endregion

        #region コンフィグ用メソッド

        /// <summary>
        /// 指定したTokensを使用して、AccessTokenを作成し、追加します。
        /// </summary>
        /// <param name="t">Tokens</param>
        public void AddToken(Tokens t)
        {
            Setting.AccessTokens.Add(new AccessToken
            {
                ScreenName = t.ScreenName,
                Token = t.AccessToken,
                TokenSecret = t.AccessTokenSecret,
                ConsumerVerifyHash = Setting.Consumer.GetHash()
            });
            Setting.SaveJson(App.ConfigurationFileName);
        }




        #endregion
    }

    #region json保存用クラスとか




    /// <summary>
    /// ユーザー情報のキャッシュを定義します。
    /// </summary>
    public class UserProfileCache
    {
        /// <summary>
        /// ユーザー名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// スクリーンネーム
        /// </summary>
        public string ScreenName { get; set; }

        /// <summary>
        /// 説明文
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 場所
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// URL
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// ツイート数
        /// </summary>
        public int Statuses { get; set; }

        /// <summary>
        /// フォロー数
        /// </summary>
        public int Friends { get; set; }

        /// <summary>
        /// フォロワー数
        /// </summary>
        public int Followers { get; set; }

        /// <summary>
        /// お気に入り数
        /// </summary>
        public int Favorites { get; set; }
    }

    #endregion
}
