using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

using Newtonsoft.Json;

using Kbtter3.ViewModels;
using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;

namespace Kbtter3.Views
{
    /* 
     * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly string ConfigFileName = "config/mainwindow.json";
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;
        MainWindowSetting setting;

        MainWindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();
            vm = DataContext as MainWindowViewModel;
            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener(vm);
            ctxlistener.Add("AccessTokenRequest", StartAccountSelect);
            ctxlistener.Add("ReplyStart", ExpandNewTweet);
            ctxlistener.Add("ToggleNewStatus", ToggleNewTweet);
            ctxlistener.Add("UserProfileImageUri", UserProfileImageUri);
            composite.Add(ctxlistener);

            vm.StatusUpdate += MainWindow_Update;
            vm.EventUpdate += vm_EventUpdate;
            if (!File.Exists(ConfigFileName)) File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(new MainWindowSetting()));
            setting = JsonConvert.DeserializeObject<MainWindowSetting>(File.ReadAllText(ConfigFileName));
            SetShortcuts();
        }

        void vm_EventUpdate(object sender, NotificationViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                ListBoxNotify.Items.Insert(0, new Frame { Content = new NotificationPage(vm) });
                if (ListBoxTimeline.Items.Count > setting.NotificationsShowMax) ListBoxNotify.Items.RemoveAt(setting.NotificationsShowMax);
            }));
        }

        private void SetShortcuts()
        {
            InputBindings.Add(new KeyBinding(vm.ToggleNewStatusCommand, new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(vm.UpdateStatusCommand, new KeyGesture(Key.Enter, ModifierKeys.Control)));
        }

        private void MainWindow_Update(object sender, StatusViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                ListBoxTimeline.Items.Insert(0, new Frame { Content = new StatusPage(vm) });
                if (ListBoxTimeline.Items.Count > setting.StatusesShowMax) ListBoxTimeline.Items.RemoveAt(setting.StatusesShowMax);
            }));
        }

        private void StartAccountSelect(object sender, PropertyChangedEventArgs e)
        {
            new AccountSelectWindow().ShowDialog();
        }

        private void UserProfileImageUri(object sender, PropertyChangedEventArgs e)
        {
            ImageUserProfileImage.Source = new BitmapImage(vm.UserProfileImageUri);
        }

        private void ExpandNewTweet(object sender, PropertyChangedEventArgs e)
        {
            ExpanderNewTweet.IsExpanded = true;
        }

        private void ToggleNewTweet(object sender, PropertyChangedEventArgs e)
        {
            ExpanderNewTweet.IsExpanded ^= true;
            if (ExpanderNewTweet.IsExpanded)
            {
                TextBoxNewTweetText.Focus();
            }
            else
            {
                ListBoxTimeline.Focus();
            }
        }
    }

    internal class MainWindowSetting
    {
        public int StatusesShowMax { get; set; }
        public int NotificationsShowMax { get; set; }

        public MainWindowSetting()
        {
            StatusesShowMax = 200;
            NotificationsShowMax = 200;
        }
    }
}
