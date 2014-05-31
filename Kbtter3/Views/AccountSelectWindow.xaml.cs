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
using Livet;
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
    /// AccountSelectWindow.xaml の相互作用ロジック
    /// </summary>
    internal partial class AccountSelectWindow : Window
    {
        LivetCompositeDisposable composite;
        PropertyChangedWeakEventListener ctxlistener;

        public AccountSelectWindow()
        {
            InitializeComponent();
            composite = new LivetCompositeDisposable();
            ctxlistener = new PropertyChangedWeakEventListener((INotifyPropertyChanged)DataContext);
            ctxlistener.Add("StartNewAccount", ExpandNewAccount);
            ctxlistener.Add("FinishNewAccount", CollapseNewAccount);
        }

        void ExpandNewAccount(object sender, PropertyChangedEventArgs e)
        {
            StackPanelNewAccount.Visibility = Visibility.Visible;
        }

        void CollapseNewAccount(object sender, PropertyChangedEventArgs e)
        {
            StackPanelNewAccount.Visibility = Visibility.Collapsed;
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}