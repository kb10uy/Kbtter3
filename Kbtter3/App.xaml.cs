using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Livet;

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

        //集約エラーハンドラ
        //private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    //TODO:ロギング処理など
        //    MessageBox.Show(
        //        "不明なエラーが発生しました。アプリケーションを終了します。",
        //        "エラー",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Error);
        //
        //    Environment.Exit(1);
        //}
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
    public class ConsumerToken
    {
        /// <summary>
        /// Consumer Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Consumer Secret
        /// </summary>
        public string Secret { get; set; }


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


    internal class MainWindowSetting
    {
        public int StatusesShowMax { get; set; }
        public int NotificationsShowMax { get; set; }
        public bool NotifyNewStatus { get; set; }
        public bool NotifyNewNotification { get; set; }
        public bool AllowJokeCommands { get; set; }
        public MainWindowSetting()
        {
            StatusesShowMax = 200;
            NotificationsShowMax = 200;
            NotifyNewStatus = false;
            NotifyNewNotification = true;
            AllowJokeCommands = false;
        }
    }

    internal class Kbtter3Setting
    {
        public Kbtter3SystemData System { get; set; }
        public MainWindowSetting MainWindow { get; set; }
        public IList<AccessToken> AccessTokens { get; set; }
        public ConsumerToken Consumer { get; set; }

        public Kbtter3Setting()
        {
            System = new Kbtter3SystemData();
            MainWindow = new MainWindowSetting();
            AccessTokens = new List<AccessToken>();
            Consumer = new ConsumerToken();
        }
    }
}
