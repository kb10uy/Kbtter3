using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
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
    public class StatusViewModel : ViewModel
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
        Status status, origin;
        static Regex reg = new Regex("<a href=\"(?<url>.+)\" rel=\"nofollow\">(?<client>.+)</a>");
        long rtid;

        public static StatusViewModel Create(Status st)
        {
            var ret = new StatusViewModel();
            ret.origin = st;
            if (st.RetweetedStatus != null)
            {
                ret._RetweetUserName = st.User.Name;
                st = st.RetweetedStatus;
                ret._IsRetweet = true;
            }
            else
            {
                ret._IsRetweet = false;
                ret._RetweetUserName = "";
            }
            ret.status = st;
            ret._UserName = st.User.Name;
            ret._ScreenName = st.User.ScreenName;
            ret._Text = st.Text;
            ret._ProfileImageUri = st.User.ProfileImageUrlHttps;
            ret._RetweetCount = st.RetweetCount;
            ret._IsFavorited = st.IsFavorited ?? false;
            ret._IsRetweeted = st.IsRetweeted ?? false;
            ret._FavoriteCount = st.FavoriteCount ?? 0;

            var vm = reg.Match(st.Source);
            ret._Via = vm.Groups["client"].Value;
            if (vm.Groups["url"].Value != "") ret._ViaUri = new Uri(vm.Groups["url"].Value);

            return ret;
        }

        public void Initialize()
        {
        }


        #region IsRetweeted変更通知プロパティ
        private bool _IsRetweeted;

        public bool IsRetweeted
        {
            get
            { return _IsRetweeted; }
            set
            {
                if (_IsRetweeted == value)
                    return;
                if (value)
                {
                    rtid = kbtter.Token.Statuses.Retweet(id => origin.Id).Id;
                }
                else
                {
                    if (rtid == 0)
                    {
                        rtid = kbtter.Token.Statuses.Show(include_my_retweet => true).CurrentUserRetweet ?? 0;
                        kbtter.Token.Statuses.Destroy(id => rtid);
                    }
                }
                _IsRetweeted = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsFavorited変更通知プロパティ
        private bool _IsFavorited;

        public bool IsFavorited
        {
            get
            { return _IsFavorited; }
            set
            {
                if (_IsFavorited == value)
                    return;
                if (value)
                {
                    kbtter.Token.Favorites.Create(id => origin.Id);
                }
                else
                {
                    kbtter.Token.Favorites.Destroy(id => origin.Id);
                }
                _IsFavorited = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region RetweetUserName変更通知プロパティ
        private string _RetweetUserName;

        public string RetweetUserName
        {
            get
            { return _RetweetUserName; }
            set
            {
                if (_RetweetUserName == value)
                    return;
                _RetweetUserName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsRetweet変更通知プロパティ
        private bool _IsRetweet;

        public bool IsRetweet
        {
            get
            { return _IsRetweet; }
            set
            {
                if (_IsRetweet == value)
                    return;
                _IsRetweet = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Via変更通知プロパティ
        private string _Via;

        public string Via
        {
            get
            { return _Via; }
            set
            {
                if (_Via == value)
                    return;
                _Via = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ViaUri変更通知プロパティ
        private Uri _ViaUri;

        public Uri ViaUri
        {
            get
            { return _ViaUri; }
            set
            {
                if (_ViaUri == value)
                    return;
                _ViaUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion



        #region UserName変更通知プロパティ
        private string _UserName;

        public string UserName
        {
            get
            { return _UserName; }
            set
            {
                if (_UserName == value)
                    return;
                _UserName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ScreenName変更通知プロパティ
        private string _ScreenName;

        public string ScreenName
        {
            get
            { return _ScreenName; }
            set
            {
                if (_ScreenName == value)
                    return;
                _ScreenName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Text変更通知プロパティ
        private string _Text;

        public string Text
        {
            get
            { return _Text; }
            set
            {
                if (_Text == value)
                    return;
                _Text = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ProfileImageUrl変更通知プロパティ
        private Uri _ProfileImageUri;

        public Uri ProfileImageUri
        {
            get
            { return _ProfileImageUri; }
            set
            {
                if (_ProfileImageUri == value)
                    return;
                _ProfileImageUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region FavoriteCount変更通知プロパティ
        private int _FavoriteCount;

        public int FavoriteCount
        {
            get
            { return _FavoriteCount; }
            set
            {
                if (_FavoriteCount == value)
                    return;
                _FavoriteCount = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region RetweetCount変更通知プロパティ
        private int _RetweetCount;

        public int RetweetCount
        {
            get
            { return _RetweetCount; }
            set
            {
                if (_RetweetCount == value)
                    return;
                _RetweetCount = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}
