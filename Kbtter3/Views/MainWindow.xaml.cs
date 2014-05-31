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
using Kbtter3.Views.Control;

using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;

namespace Kbtter3.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    internal partial class MainWindow : Window
    {
        public static readonly string ConfigFileName = "config/mainwindow.json";
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;
        MainWindowSetting setting;

        internal event MainWindowEventHandler WindowEvent;
        MainWindowViewModel vm;
        int urs = 0, urn = 0;

        public MainWindow()
        {
            InitializeComponent();
            vm = DataContext as MainWindowViewModel;
            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener(vm);
            ctxlistener.Add("AccessTokenRequest", StartAccountSelect);
            ctxlistener.Add("ReplyStart", ExpandNewTweet);
            ctxlistener.Add("ToggleNewStatus", ToggleNewTweet);
            composite.Add(ctxlistener);

            vm.StatusUpdate += MainWindow_Update;
            vm.EventUpdate += vm_EventUpdate;
            if (!File.Exists(ConfigFileName)) File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(new MainWindowSetting()));
            setting = JsonConvert.DeserializeObject<MainWindowSetting>(File.ReadAllText(ConfigFileName));
            SetShortcuts();
            WindowEvent += MainWindow_WindowEvent;
        }

        void MainWindow_WindowEvent(string target, string type, object msg)
        {

        }

        private void StartAccountSelect(object sender, PropertyChangedEventArgs e)
        {
            new AccountSelectWindow().ShowDialog();
        }

        #region Status・Event受信
        void vm_EventUpdate(object sender, NotificationViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                if (TabControlMain.SelectedIndex != 1 && setting.NotifyNewNotification)
                {
                    EmphasisTextBlock(TextBlockNotification);
                    urn++;
                    TextBlockUnreadNotifications.Text = String.Format(" {0}", urn);
                }
                ListBoxNotify.Items.Insert(0, new Frame { Content = new NotificationPage(vm) });
                if (ListBoxTimeline.Items.Count > setting.NotificationsShowMax) ListBoxNotify.Items.RemoveAt(setting.NotificationsShowMax);
            }));
        }

        private void MainWindow_Update(object sender, StatusViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                if (TabControlMain.SelectedIndex != 0 && setting.NotifyNewStatus)
                {
                    EmphasisTextBlock(TextBlockTimeline);
                    urs++;
                    TextBlockUnreadStatuses.Text = String.Format(" {0}", urs);
                }
                ListBoxTimeline.Items.Insert(0, new Frame { Content = new StatusPage(this, vm) });
                if (ListBoxTimeline.Items.Count > setting.StatusesShowMax) ListBoxTimeline.Items.RemoveAt(setting.StatusesShowMax);
            }));
        }
        #endregion


        #region ショートカット
        private void SetShortcuts()
        {
            InputBindings.Add(new KeyBinding(vm.ToggleNewStatusCommand, new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(vm.UpdateStatusCommand, new KeyGesture(Key.Enter, ModifierKeys.Control)));
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
        #endregion


        #region UIロジック
        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (TabControlMain.SelectedIndex)
            {
                case 0:
                    urs = 0;
                    TextBlockUnreadStatuses.Text = "";
                    DisemphasisTextBlock(TextBlockTimeline);
                    break;
                case 1:
                    urn = 0;
                    TextBlockUnreadNotifications.Text = "";
                    DisemphasisTextBlock(TextBlockNotification);
                    break;
                default:
                    break;
            }
        }

        private void EmphasisTextBlock(TextBlock tb)
        {
            tb.Foreground = Brushes.Red;
            tb.FontWeight = FontWeights.Bold;
        }

        private void DisemphasisTextBlock(TextBlock tb)
        {
            tb.Foreground = Brushes.Black;
            tb.FontWeight = FontWeights.Normal;
        }

        public void AddTab(UIElement header, UIElement elm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                var tp = new ClosableRoundTabItem();
                tp.Header = header;
                tp.Content = elm;
                tp.Closed += (s, e) =>
                {
                    TabControlMain.Items.Remove(s);
                };
                TabControlMain.Items.Add(tp);
            }));
        }

        public void AddTab(string header, UIElement elm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                var tp = new ClosableRoundTabItem();
                tp.Header = header;
                tp.Content = elm;
                tp.Closed += (s, e) =>
                {
                    TabControlMain.Items.Remove(s);
                };
                TabControlMain.Items.Add(tp);
            }));
        }

        private void MenuAllowTextWrapping_Click(object sender, RoutedEventArgs e)
        {
            //ListBoxTimeline.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, MenuAllowTextWrapping.IsChecked ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
            //RaiseWindowEvent(WindowEventTargetStatusPage, MenuAllowTextWrapping.IsChecked.ToString());
        }
        #endregion


        #region View間連携
        public void RequestAction(string type, string info)
        {
            switch (type)
            {
                case "Url":
                    break;
                case "Media":
                    break;
                case "Mention":
                    var upvm = vm.GetUserProfile(info);
                    //ここは取得するようにしたほうがいいかなーとか
                    if (upvm == null) return;
                    AddTab(String.Format("{0}さんの情報", info), new Frame { Content = new UserProfilePage(this, upvm) });
                    break;
                case "Hashtag":
                    break;
                default:
                    throw new InvalidOperationException(String.Format("不明なリクエストです:{0}", type));
            }
        }

        public static readonly string WindowEventTargetStatusPage = "StatusPage";

        public void RaiseWindowEvent(string target, string type, string typeval)
        {
            WindowEvent(target, type + "-" + typeval, null);
        }

        public void RaiseWindowEvent(string target, string type, object obj)
        {
            WindowEvent(target, type, obj);
        }

        public void RaiseWindowEvent(string target, string type, string typeval, object obj)
        {
            WindowEvent(target, type + "-" + typeval, obj);
        }

        public void RaiseWindowEvent(string type, string typeval)
        {
            WindowEvent("Global", type + "-" + typeval, null);
        }
        #endregion

    }

    internal delegate void MainWindowEventHandler(string target, string type, object obj);

    internal class MainWindowSetting
    {
        public int StatusesShowMax { get; set; }
        public int NotificationsShowMax { get; set; }
        public bool NotifyNewStatus { get; set; }
        public bool NotifyNewNotification { get; set; }

        public MainWindowSetting()
        {
            StatusesShowMax = 200;
            NotificationsShowMax = 200;
            NotifyNewStatus = false;
            NotifyNewNotification = true;
        }
    }
}
