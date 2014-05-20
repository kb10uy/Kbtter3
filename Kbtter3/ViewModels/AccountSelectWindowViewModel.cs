using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Kbtter3.Models;

namespace Kbtter3.ViewModels
{
    public class AccountSelectWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        Kbtter kbtter = Kbtter.Instance;

        public AccountSelectWindowViewModel()
        {
            Accounts = new ObservableCollection<string>();
        }

        public void Initialize()
        {
            foreach (var i in kbtter.AccessTokens)
            {
                Accounts.Add(String.Format("@{0}", i.ScreenName));
            }
        }


        #region Accounts変更通知プロパティ
        private ObservableCollection<string> _Accounts;

        public ObservableCollection<string> Accounts
        {
            get
            { return _Accounts; }
            set
            {
                if (_Accounts == value)
                    return;
                _Accounts = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region EnteredPinCode変更通知プロパティ
        private string _EnteredPinCode = "";

        public string EnteredPinCode
        {
            get
            { return _EnteredPinCode; }
            set
            {
                if (_EnteredPinCode == value)
                    return;
                _EnteredPinCode = value;
                RaisePropertyChanged(() => EnteredPinCode);
                RegisterNewAccountCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion


        #region RegisterNewAccountCommand
        private ViewModelCommand _RegisterNewAccountCommand;

        public ViewModelCommand RegisterNewAccountCommand
        {
            get
            {
                if (_RegisterNewAccountCommand == null)
                {
                    _RegisterNewAccountCommand = new ViewModelCommand(RegisterNewAccount, CanRegisterNewAccount);
                }
                return _RegisterNewAccountCommand;
            }
        }

        public bool CanRegisterNewAccount()
        {
            return EnteredPinCode.Length == 7 && narstart;
        }

        public void RegisterNewAccount()
        {
            narstart = false;
            var t = kbtter.AuthorizeToken(EnteredPinCode);
            if (t != null)
            {
                kbtter.AddToken(t);
                Accounts.Add(String.Format("@{0}", t.ScreenName));
            }
            RaisePropertyChanged("FinishNewAccount");
            StartNewAccountCommand.RaiseCanExecuteChanged();
        }
        #endregion


        #region StartNewAccountCommand
        bool narstart = false;
        private ViewModelCommand _StartNewAccountCommand;

        public ViewModelCommand StartNewAccountCommand
        {
            get
            {
                if (_StartNewAccountCommand == null)
                {
                    _StartNewAccountCommand = new ViewModelCommand(StartNewAccount, CanStartNewAccount);
                }
                return _StartNewAccountCommand;
            }
        }

        public bool CanStartNewAccount()
        {
            return !narstart;
        }

        public void StartNewAccount()
        {
            narstart = true;
            Process.Start(kbtter.OAuthSession.AuthorizeUri.ToString());

            RaisePropertyChanged("StartNewAccount");
            StartNewAccountCommand.RaiseCanExecuteChanged();
        }
        #endregion


        #region LoginCommand
        private ViewModelCommand _LoginCommand;

        public ViewModelCommand LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                {
                    _LoginCommand = new ViewModelCommand(Login, CanLogin);
                }
                return _LoginCommand;
            }
        }

        public bool CanLogin()
        {
            return selac != -1;
        }

        public void Login()
        {
            kbtter.AuthenticateWith(selac);
            kbtter.StartStreaming();
        }
        #endregion

        #region アカウント選択
        int selac = -1;

        public void SelectAccount(int ac)
        {
            selac = ac;
            LoginCommand.RaiseCanExecuteChanged();
        }
        #endregion


        #region ExitCommand
        private ViewModelCommand _ExitCommand;

        public ViewModelCommand ExitCommand
        {
            get
            {
                if (_ExitCommand == null)
                {
                    _ExitCommand = new ViewModelCommand(Exit);
                }
                return _ExitCommand;
            }
        }

        public void Exit()
        {
            App.Current.Shutdown();
        }
        #endregion

    }
}
