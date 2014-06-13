using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;
using IOPath = System.IO.Path;
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
        public static readonly string ClipboardImageSavingFolderName = "clipimage";
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;
        Kbtter3Setting setting;

        MainWindowViewModel vm;
        int urs = 0, urn = 0;
        bool imeing = false;

        public MainWindow()
        {
            InitializeComponent();

            vm = DataContext as MainWindowViewModel;
            composite = new LivetCompositeDisposable();

            ctxlistener = new PropertyChangedWeakEventListener(vm);
            ctxlistener.Add("AccessTokenRequest", StartAccountSelect);
            ctxlistener.Add("ReplyStart", ExpandNewTweet);
            ctxlistener.Add("ToggleNewStatus", ToggleNewTweet);
            ctxlistener.Add("StatusAction", (s, e) => RequestAction(vm.StatusAction.Type, vm.StatusAction.Information));
            composite.Add(ctxlistener);

            vm.StatusUpdate += MainWindow_Update;
            vm.EventUpdate += vm_EventUpdate;
            setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);

            if (!setting.MainWindow.AllowJokeCommands) ToolBarJokes.Visibility = Visibility.Collapsed;

            SetShortcuts();
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
                if (TabControlMain.SelectedIndex != 1 && setting.MainWindow.NotifyNewNotification)
                {
                    EmphasisTextBlock(TextBlockNotification);
                    urn++;
                    TextBlockUnreadNotifications.Text = String.Format(" {0}", urn);
                }
                ListBoxNotify.Items.Insert(0, new Frame { Content = new NotificationPage(vm) });
                if (ListBoxTimeline.Items.Count > setting.MainWindow.NotificationsShowMax) ListBoxNotify.Items.RemoveAt(setting.MainWindow.NotificationsShowMax);
            }));
        }

        private void MainWindow_Update(object sender, StatusViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                if (TabControlMain.SelectedIndex != 0 && setting.MainWindow.NotifyNewStatus)
                {
                    EmphasisTextBlock(TextBlockTimeline);
                    urs++;
                    TextBlockUnreadStatuses.Text = String.Format(" {0}", urs);
                }
                ListBoxTimeline.Items.Insert(0, new Frame { Content = new StatusPage(vm) });
                if (ListBoxTimeline.Items.Count > setting.MainWindow.StatusesShowMax) ListBoxTimeline.Items.RemoveAt(setting.MainWindow.StatusesShowMax);
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


        public async void RequestAction(string type, string info)
        {
            switch (type)
            {
                case "Url":
                case "Media":
                    Process.Start(info);
                    break;
                case "Mention":
                    var upvm = await vm.GetUserProfile(info);
                    if (upvm == null) return;
                    AddTab(new TextBlock { Text = String.Format("{0}さんの情報", info) },
                           new Frame { Content = new UserProfilePage(this, upvm) });
                    break;
                case "Hashtag":
                    break;
                default:
                    throw new InvalidOperationException(String.Format("不明なリクエストです:{0}", type));
            }
        }
        #endregion



        private void ButtonSendClipImage_Click(object sender, RoutedEventArgs e)
        {

            if (!Clipboard.ContainsImage()) return;
            var b = Clipboard.GetImage();
            b.Dispatcher.BeginInvoke(new Action(() =>
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(b));
                if (!Directory.Exists(ClipboardImageSavingFolderName)) Directory.CreateDirectory(ClipboardImageSavingFolderName);
                var sdt = DateTime.Now;

                var fn = IOPath.Combine(IOPath.GetFullPath(ClipboardImageSavingFolderName),
                    String.Format("cbimg_{0:D4}-{1:D2}-{2:D2}_{3:D2}-{4:D2}-{5:D2}.png",
                                    sdt.Year,
                                    sdt.Month,
                                    sdt.Day,
                                    sdt.Hour,
                                    sdt.Minute,
                                    sdt.Second
                    ));

                using (var fs = new FileStream(fn, FileMode.Create))
                {
                    enc.Save(fs);
                }
                vm.AddMediaDirect(fn);
                ExpanderNewTweet.IsExpanded = true;
            }));
        }

        private void TextBlockPopupNotification_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property != TextBlock.TextProperty) return;
            var sb = TextBlockPopupNotification.TryFindResource("Storyboard") as Storyboard;
            sb.Begin();
        }

        private void MenuSetting_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new TextBlock { Text = "設定" },
                           new Frame { Content = new SettingPage(), NavigationUIVisibility = NavigationUIVisibility.Hidden });
        }

    }

    internal delegate void MainWindowEventHandler(string target, string type, object obj);

}
