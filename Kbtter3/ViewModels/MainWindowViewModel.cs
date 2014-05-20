using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Kbtter3.Models;
using CoreTweet;

namespace Kbtter3.ViewModels
{
    public class MainWindowViewModel : ViewModel, IDataErrorInfo
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

        Kbtter kbtter;
        PropertyChangedEventListener listener;
        public event StatusUpdateEventHandler Update;
        Dictionary<string, string> errors = new Dictionary<string, string>();

        //public Queue<StatusViewModel> Statuses { get; protected set; }
        //public Status SelectedStatus { get; internal set; }

        public void Initialize()
        {
            kbtter = Kbtter.Instance;
            listener = new PropertyChangedEventListener(kbtter);
            CompositeDisposable.Add(listener);
            RegisterHandlers();

            kbtter.Initialize();
            //Statuses = new ObservableSynchronizedCollection<StatusViewModel>();
        }

        public void RegisterHandlers()
        {
            listener.Add("AccessTokenRequest", OnAccessTokenRequest);
            listener.Add("Status", OnStatusUpdate);
        }

        public void OnAccessTokenRequest(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("AccessTokenRequest");
        }

        public void OnStatusUpdate(object sender, PropertyChangedEventArgs e)
        {
            if (Update != null) Update(this, StatusViewModel.Create(kbtter.ShowingStatuses.Dequeue()));

        }


        #region UpdateStatusText変更通知プロパティ
        private string _UpdateStatusText = "";

        public string UpdateStatusText
        {
            get
            { return _UpdateStatusText; }
            set
            {
                if (_UpdateStatusText == value)
                    return;
                _UpdateStatusText = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => UpdateStatusTextLength);

                errors["UpdateStatusText"] = (value.Length > 140) ? "140文字を超えています" : null;
            }
        }
        #endregion


        #region UpdateStatusTextLength変更通知プロパティ

        public int UpdateStatusTextLength
        {
            get
            { return 140 - _UpdateStatusText.Length; }
        }
        #endregion



        #region UpdateStatusCommand
        private ViewModelCommand _UpdateStatusCommand;
        bool _tokenus = false;

        public ViewModelCommand UpdateStatusCommand
        {
            get
            {
                if (_UpdateStatusCommand == null)
                {
                    _UpdateStatusCommand = new ViewModelCommand(UpdateStatus, CanUpdateStatus);
                }
                return _UpdateStatusCommand;
            }
        }

        public bool CanUpdateStatus()
        {
            return UpdateStatusText.Length <= 140 && !_tokenus;
        }

        public async void UpdateStatus()
        {
            _tokenus = true;
            UpdateStatusCommand.RaiseCanExecuteChanged();

            await kbtter.Token.Statuses.UpdateAsync(status => UpdateStatusText);

            _tokenus = false;
            UpdateStatusCommand.RaiseCanExecuteChanged();
            UpdateStatusText = "";
        }
        #endregion


        #region エラー
        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string columnName]
        {
            get
            {
                if (errors.ContainsKey(columnName))
                {
                    return errors[columnName];
                }
                return null;
            }
        }
        #endregion
    }

    public delegate void StatusUpdateEventHandler(object sender, StatusViewModel vm);
}
