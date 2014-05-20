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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

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

        public MainWindow()
        {
            InitializeComponent();
            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener((INotifyPropertyChanged)DataContext);
            ctxlistener.Add("AccessTokenRequest", StartAccountSelect);
            composite.Add(ctxlistener);

            ((MainWindowViewModel)DataContext).Update += MainWindow_Update;
        }

        void MainWindow_Update(object sender, StatusViewModel vm)
        {
            DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() =>
            {
                ListBoxTimeline.Items.Insert(0, new Frame { Content = new StatusPage(vm) });
            }));
        }

        void StartAccountSelect(object sender, PropertyChangedEventArgs e)
        {
            new AccountSelectWindow().ShowDialog();
        }

    }
}
