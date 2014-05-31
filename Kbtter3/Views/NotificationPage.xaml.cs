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

using Kbtter3.ViewModels;

using Livet;
using Livet.EventListeners;
using Livet.EventListeners.WeakEvents;
using System.ComponentModel;

namespace Kbtter3.Views
{

    /// <summary>
    /// StatusPage.xaml の相互作用ロジック
    /// </summary>
    internal partial class NotificationPage : Page
    {
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;
        NotificationViewModel vm;
        Storyboard sb;
        bool showed = false;

        public NotificationPage(NotificationViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;
            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener((INotifyPropertyChanged)DataContext);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (showed) return;
            showed = true;

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

        }

        private void Hyperlink_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Hyperlink).Foreground = Brushes.Red;
        }

        private void Hyperlink_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Hyperlink).Foreground = Brushes.DodgerBlue;
        }

    }
}