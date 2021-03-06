using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Livet;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;

namespace Kbtter3
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    internal partial class App : Application
    {
        /// <summary>
        /// コンフィグファイルが保存されているフォルダの名前を取得します。
        /// </summary>
        public static readonly string ConfigurationFolderName = "config";

        public static readonly string LoggingFileName = "Kbtter3.log";

        /// <summary>
        /// 設定ファイルのパス
        /// </summary>
        public static readonly string ConfigurationFileName = ConfigurationFolderName + "/config.json";

        internal readonly string ConsumerDefaultKey = "5bI3XiTNEMHiamjMV5Acnqkex";
        internal readonly string ConsumerDefaultSecret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            DispatcherHelper.UIDispatcher = Dispatcher;
            if (!Directory.Exists(ConfigurationFolderName)) Directory.CreateDirectory(ConfigurationFolderName);

            var Setting = new Kbtter3Setting();
            Setting.Consumer = new ConsumerToken { Key = ConsumerDefaultKey, Secret = ConsumerDefaultSecret };
            Setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName, Setting);
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }
        /*
        //集約エラーハンドラ
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //TODO:ロギング処理など
            MessageBox.Show(
                "不明なエラーが発生しました。アプリケーションを終了します。\n" + (e.ExceptionObject as Exception).Message,
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Environment.Exit(1);
        }
        */
    }


    internal static class Kbtter3Extension
    {
        public static T LoadJson<T>(string filename)
            where T : new()
        {
            if (!File.Exists(filename))
            {
                var o = new T();
                File.WriteAllText(filename, JsonConvert.SerializeObject(o, Formatting.Indented));
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
        }

        public static T LoadJson<T>(string filename, T def)
        {
            if (!File.Exists(filename))
            {
                File.WriteAllText(filename, JsonConvert.SerializeObject(def, Formatting.Indented));
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
        }

        public static void SaveJson<T>(this T obj, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        public static bool EndsWith(this string t, string es)
        {
            return t.IndexOf(es) == (t.Length - es.Length);
        }

        public static T CloneViaJson<T>(T obj)
            where T : class
        {
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj)) as T;
        }
        
        //http://d.hatena.ne.jp/hilapon/20120301/1330569751
        public static T DeepCopy<T>(this T source) where T : class
        {
            T result;
            try
            {
                var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
                using (var mem = new System.IO.MemoryStream())
                {
                    serializer.WriteObject(mem, source);
                    mem.Position = 0;
                    result = serializer.ReadObject(mem) as T;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return result;
        }
    }

    /// <summary>
    /// TwitterにOAuthでログインする際に必要なAccessTokenを
    /// 定義します。
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// スクリーンネーム
        /// </summary>
        public string ScreenName { get; set; }

        /// <summary>
        /// Access Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Access Token Secret
        /// </summary>
        public string TokenSecret { get; set; }

        /// <summary>
        /// このAccessTokenが作成された時のConsumerTokenのHash。
        /// </summary>
        public string ConsumerVerifyHash { get; set; }
    }

    /// <summary>
    /// TwitterにOAuthでログインする際に必要なConsumerKey/Secretの組を
    /// 定義します。
    /// </summary>
    public class ConsumerToken : NotificationObject
    {

        #region Key変更通知プロパティ
        private string _Key;

        /// <summary>
        /// Consumer Key
        /// </summary>
        public string Key
        {
            get
            { return _Key; }
            set
            {
                if (_Key == value)
                    return;
                _Key = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Secret変更通知プロパティ
        private string _Secret;

        /// <summary>
        /// Consumer Secret
        /// </summary>
        /// <returns></returns>
        public string Secret
        {
            get
            { return _Secret; }
            set
            {
                if (_Secret == value)
                    return;
                _Secret = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// 使うな
        /// </summary>
        /// <returns>なし</returns>
        public string GetHash()
        {
            return new String((Key + Secret).Where((p, i) => i % 7 == 0 || i % 5 == 0).ToArray());
        }
    }

    internal class Kbtter3SystemData
    {
        public long LastFavoritedStatusId { get; set; }
        public long LastRetweetedStatusId { get; set; }
        public IDictionary<string, int> BeamCount { get; set; }
        public IDictionary<string, int> HateCount { get; set; }
        public IDictionary<string, int> GodCount { get; set; }

        public Kbtter3SystemData()
        {
            LastFavoritedStatusId = 0;
            LastRetweetedStatusId = 0;
            BeamCount = new Dictionary<string, int>();
            HateCount = new Dictionary<string, int>();
            GodCount = new Dictionary<string, int>();
        }
    }

    internal class MainWindowSetting : NotificationObject
    {

        #region StatusesShowMax変更通知プロパティ
        private int _StatusesShowMax;

        public int StatusesShowMax
        {
            get
            { return _StatusesShowMax; }
            set
            {
                if (_StatusesShowMax == value)
                    return;
                _StatusesShowMax = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region NotificationsShowMax変更通知プロパティ
        private int _NotificationsShowMax;

        public int NotificationsShowMax
        {
            get
            { return _NotificationsShowMax; }
            set
            {
                if (_NotificationsShowMax == value)
                    return;
                _NotificationsShowMax = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region NotifyNewStatus変更通知プロパティ
        private bool _NotifyNewStatus;

        public bool NotifyNewStatus
        {
            get
            { return _NotifyNewStatus; }
            set
            {
                if (_NotifyNewStatus == value)
                    return;
                _NotifyNewStatus = value;
                RaisePropertyChanged();
            }
        }
        #endregion   

        #region NotifyNewNotification変更通知プロパティ
        private bool _NotifyNewNotification;

        public bool NotifyNewNotification
        {
            get
            { return _NotifyNewNotification; }
            set
            {
                if (_NotifyNewNotification == value)
                    return;
                _NotifyNewNotification = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region AllowJokeCommands変更通知プロパティ
        private bool _AllowJokeCommands;

        public bool AllowJokeCommands
        {
            get
            { return _AllowJokeCommands; }
            set
            {
                if (_AllowJokeCommands == value)
                    return;
                _AllowJokeCommands = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public MainWindowSetting()
        {
            StatusesShowMax = 200;
            NotificationsShowMax = 200;
            NotifyNewStatus = false;
            NotifyNewNotification = true;
            AllowJokeCommands = false;
        }
    }

    internal class StatusPageSetting : NotificationObject
    {

        #region AnimationNewStatus変更通知プロパティ
        private bool _AnimationNewStatus;

        public bool AnimationNewStatus
        {
            get
            { return _AnimationNewStatus; }
            set
            {
                if (_AnimationNewStatus == value)
                    return;
                _AnimationNewStatus = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ShowAbsoluteTime変更通知プロパティ
        private bool _ShowAbsoluteTime;

        public bool ShowAbsoluteTime
        {
            get
            { return _ShowAbsoluteTime; }
            set
            {
                if (_ShowAbsoluteTime == value)
                    return;
                _ShowAbsoluteTime = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public StatusPageSetting()
        {
            AnimationNewStatus = true;
            ShowAbsoluteTime = false;
        }
    }

    internal class UserProfilePageSetting : NotificationObject
    {

        #region StatusesShowCount変更通知プロパティ
        private int _StatusesShowCount;

        public int StatusesShowCount
        {
            get
            { return _StatusesShowCount; }
            set
            {
                if (_StatusesShowCount == value)
                    return;
                _StatusesShowCount = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public UserProfilePageSetting()
        {
            StatusesShowCount = 20;
        }
    }

    internal class Kbtter3Setting : NotificationObject
    {

        #region System変更通知プロパティ
        private Kbtter3SystemData _System;

        public Kbtter3SystemData System
        {
            get
            { return _System; }
            set
            {
                if (_System == value)
                    return;
                _System = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region MainWindow変更通知プロパティ
        private MainWindowSetting _MainWindow;

        public MainWindowSetting MainWindow
        {
            get
            { return _MainWindow; }
            set
            {
                if (_MainWindow == value)
                    return;
                _MainWindow = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region StatusPage変更通知プロパティ
        private StatusPageSetting _StatusPage;

        public StatusPageSetting StatusPage
        {
            get
            { return _StatusPage; }
            set
            {
                if (_StatusPage == value)
                    return;
                _StatusPage = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region UserProfilePage変更通知プロパティ
        private UserProfilePageSetting _UserProfilePage;

        public UserProfilePageSetting UserProfilePage
        {
            get
            { return _UserProfilePage; }
            set
            {
                if (_UserProfilePage == value)
                    return;
                _UserProfilePage = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region AccessTokens変更通知プロパティ
        private ObservableCollection<AccessToken> _AccessTokens;

        public ObservableCollection<AccessToken> AccessTokens
        {
            get
            { return _AccessTokens; }
            set
            {
                if (_AccessTokens == value)
                    return;
                _AccessTokens = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Consumer変更通知プロパティ
        private ConsumerToken _Consumer;

        public ConsumerToken Consumer
        {
            get
            { return _Consumer; }
            set
            {
                if (_Consumer == value)
                    return;
                _Consumer = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public Kbtter3Setting()
        {
            System = new Kbtter3SystemData();
            MainWindow = new MainWindowSetting();
            StatusPage = new StatusPageSetting();
            UserProfilePage = new UserProfilePageSetting();
            AccessTokens = new ObservableCollection<AccessToken>();
            Consumer = new ConsumerToken();
        }
    }
}
