using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Diagnostics;
using System.Windows.Interactivity;

using Kbtter3.ViewModels;
using Newtonsoft.Json;

using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;
using System.ComponentModel;

namespace Kbtter3.Views
{

    /// <summary>
    /// StatusPage.xaml の相互作用ロジック
    /// </summary>
    internal partial class StatusPage : Page
    {
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;

        Storyboard sb;
        bool showed = false;
        IList<StatusViewModel.StatusElement> elm;
        string stsn;
        StatusViewModel statvm;

        static Kbtter3Setting setting;

        static StatusPage()
        {
            setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
        }

        public StatusPage(StatusViewModel vm)
        {
            statvm = vm;
            InitializeComponent();
            DataContext = vm;
            elm = vm.TextElements;
            stsn = vm.ScreenName;

            if (vm.Via == "") StackPanelBlockVia.Visibility = Visibility.Collapsed;
            SetMainText();

            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener((INotifyPropertyChanged)DataContext);
            ctxlistener.Add("Delete", DeleteContent);
        }

        ~StatusPage()
        {
            Debug.WriteLine("Statuspage has been deleted!");
        }

        private void DeleteContent(object s, PropertyChangedEventArgs e)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(() => Content = new Label { Content = "このツイートは削除されました" });
        }

        private void SetMainText()
        {
            foreach (var i in elm)
            {
                switch (i.Type)
                {
                    case "None":
                        TextBlockMainText.Inlines.Add(i.Text);
                        break;

                    case "Url":
                    case "Media":
                    case "Mention":
                    case "Hashtag":
                        var hl = new Hyperlink();
                        hl.Inlines.Add(i.Text);
                        hl.Tag = i;
                        hl.Click += (s, e2) =>
                        {
                            var t = ((s as Hyperlink).Tag as StatusViewModel.StatusElement);
                            RequestHyperlinkAction(t.Type, t.Infomation);
                            e2.Handled = true;
                        };
                        var bh = new HyperlinkMouseOverColorChangeBehavior();
                        bh.MouseEnteredForeground = Brushes.Red;
                        bh.MouseLeftForeground = Brushes.DodgerBlue;
                        Interaction.GetBehaviors(hl).Add(bh);
                        TextBlockMainText.Inlines.Add(hl);
                        break;
                    default:
                        throw new InvalidOperationException("予期しないテキスト要素です");
                }
            }

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (showed) return;
            showed = true;
            if (!setting.StatusPage.AnimationNewStatus) return;

            var cdu = new Duration(TimeSpan.FromMilliseconds(500));
            //たかさ
            var dah = new DoubleAnimation { From = 0.0, To = this.ActualHeight, Duration = cdu, EasingFunction = new SineEase() };
            Storyboard.SetTargetProperty(dah, new PropertyPath("Height"));
            Storyboard.SetTarget(dah, this);
            //透明度
            var dao = new DoubleAnimation { From = 0, To = 1, Duration = cdu };
            Storyboard.SetTargetProperty(dao, new PropertyPath("Opacity"));
            Storyboard.SetTarget(dao, this);

            sb = new Storyboard();
            sb.Children.Add(dah);
            sb.Children.Add(dao);
            this.Height = 0;
            sb.Begin();

        }

        private void RequestHyperlinkAction(string type, string info)
        {
            statvm.main.RequestStatusAction(type, info);
        }

        //Via用とか
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            RequestHyperlinkAction("Url", e.Uri.ToString());
            e.Handled = true;
        }


        private void HyperlinkScreenName_Click(object sender, RoutedEventArgs e)
        {
            RequestHyperlinkAction("Mention", stsn);
            e.Handled = true;
        }

    }
}