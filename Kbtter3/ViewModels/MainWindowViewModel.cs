using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Kbtter3.Models;
using CoreTweet;
using CoreTweet.Streaming;

namespace Kbtter3.ViewModels
{
    internal class MainWindowViewModel : ViewModel, IDataErrorInfo
    {

        Kbtter kbtter;
        PropertyChangedEventListener listener;
        public event EventUpdateEventHandler EventUpdate;
        Dictionary<string, string> errors = new Dictionary<string, string>();
        Kbtter3Setting setting;

        public void Initialize()
        {
            kbtter = Kbtter.Instance;
            listener = new PropertyChangedEventListener(kbtter);
            CompositeDisposable.Add(listener);
            RegisterHandlers();
            kbtter.Initialize();
            setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
            TimelineStatuses.CollectionChanged += (s, e) =>
            {
                DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
                {
                    if (TimelineStatuses.Count > setting.MainWindow.StatusesShowMax)
                    {
                        TimelineStatuses.RemoveAt(setting.MainWindow.StatusesShowMax);
                    }
                }));
            };
            TimelineNotifications.CollectionChanged += (s, e) =>
            {
                DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
                {
                    if (TimelineNotifications.Count > setting.MainWindow.NotificationsShowMax)
                    {
                        TimelineNotifications.RemoveAt(setting.MainWindow.NotificationsShowMax);
                    }
                }));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CompositeDisposable.Dispose();
        }

        public void RegisterHandlers()
        {
            listener.Add("AccessTokenRequest", OnAccessTokenRequest);
            listener.Add("PluginErrorCount", (s, e) =>
            {
                NotifyInformation(
                    string.Format(
                        "{0}個のプラグインがエラーで読み込めませんでした。確認してください。",
                        kbtter.PluginErrorCount
                    ));
            });
            listener.Add("Status", OnStatusUpdate);
            listener.Add("Event", OnEvent);
            listener.Add("DirectMessage", OnDirectMessageUpdate);
            listener.Add("AuthenticatedUser", OnUserProfileUpdate);
        }

        public void OnAccessTokenRequest(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("AccessTokenRequest");
        }

        public void OnStatusUpdate(object sender, PropertyChangedEventArgs e)
        {
            var st = kbtter.LatestStatus.Status;
            TimelineStatuses.Insert(0, StatusViewModelExtension.CreateStatusViewModel(this, st));
            RaisePropertyChanged("StatusUpdate");
            if (st.RetweetedStatus != null)
            {
                if (st.RetweetedStatus.User.Id != kbtter.AuthenticatedUser.Id) return;
                var vm = new NotificationViewModel(st);
                TimelineNotifications.Insert(0, vm);
                //if (EventUpdate != null) EventUpdate(this, vm);
                return;
            }
            if (st.Entities != null && st.Entities.UserMentions.Count(p => p.ScreenName == kbtter.AuthenticatedUser.ScreenName) != 0)
            {
                var vm = new NotificationViewModel(st, this);
                TimelineNotifications.Insert(0, vm);
                //if (EventUpdate != null) EventUpdate(this, vm);
            }
        }

        public void OnEvent(object sender, PropertyChangedEventArgs e)
        {
            if (kbtter.LatestEvent.Target.Id != kbtter.AuthenticatedUser.Id) return;
            var vm = new NotificationViewModel(kbtter.LatestEvent);
            TimelineNotifications.Insert(0, vm);
            RaisePropertyChanged("NotificationUpdate");
            //if (EventUpdate != null) EventUpdate(this, vm);
        }

        public void OnUserProfileUpdate(object sender, PropertyChangedEventArgs e)
        {
            UserProfileImageUri = kbtter.AuthenticatedUser.ProfileImageUrlHttps;
            UserProfileStatuses = kbtter.AuthenticatedUser.StatusesCount;
            UserProfileFriends = kbtter.AuthenticatedUser.FriendsCount;
            UserProfileFollowers = kbtter.AuthenticatedUser.FollowersCount;
            UserProfileFavorites = kbtter.AuthenticatedUser.FavouritesCount;
        }

        public void OnDirectMessageUpdate(object sender, PropertyChangedEventArgs e)
        {
            var dm = kbtter.LatestDirectMessage.DirectMessage;
            var dml = DirectMessages.FirstOrDefault(p => p.CheckUserPair(dm.Recipient.ScreenName, dm.Sender.ScreenName));
            if (dml == null)
            {
                var nd = new DirectMessageViewModel(this, kbtter.AuthenticatedUser.ScreenName);
                User tus;
                if (dm.Recipient.Id == kbtter.AuthenticatedUser.Id)
                {
                    tus = dm.Sender;
                }
                else
                {
                    tus = dm.Recipient;
                }
                nd.TargetUserName = tus.Name;
                nd.TargetUserScreenName = tus.ScreenName;
                nd.TargetUserImageUri = tus.ProfileImageUrlHttps;
                DirectMessages.Add(nd);
                dml = nd;
            }
            dml.AddMessage(dm);
            RaisePropertyChanged("DirectMessage");
        }

        public async Task<UserProfilePageViewModel> GetUserProfile(string sn)
        {
            var t = await kbtter.GetUser(sn);
            if (t == null)
            {
                return null;
            }
            return new UserProfilePageViewModel(t, this);
        }


        #region TimelineStatuses変更通知プロパティ
        private ObservableSynchronizedCollection<StatusViewModel> _TimelineStatuses = new ObservableSynchronizedCollection<StatusViewModel>();

        public ObservableSynchronizedCollection<StatusViewModel> TimelineStatuses
        {
            get
            { return _TimelineStatuses; }
            set
            {
                if (_TimelineStatuses == value)
                    return;
                _TimelineStatuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region TimelineNotifications変更通知プロパティ
        private ObservableSynchronizedCollection<NotificationViewModel> _TimelineNotifications = new ObservableSynchronizedCollection<NotificationViewModel>();

        public ObservableSynchronizedCollection<NotificationViewModel> TimelineNotifications
        {
            get
            { return _TimelineNotifications; }
            set
            {
                if (_TimelineNotifications == value)
                    return;
                _TimelineNotifications = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserProfileImageUri変更通知プロパティ
        private Uri _UserProfileImageUri = new Uri("", UriKind.RelativeOrAbsolute);

        public Uri UserProfileImageUri
        {
            get
            { return _UserProfileImageUri; }
            set
            {
                if (_UserProfileImageUri == value)
                    return;
                _UserProfileImageUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserProfileStatuses変更通知プロパティ
        private int _UserProfileStatuses;

        public int UserProfileStatuses
        {
            get
            { return _UserProfileStatuses; }
            set
            {
                if (_UserProfileStatuses == value)
                    return;
                _UserProfileStatuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserProfileFriends変更通知プロパティ
        private int _UserProfileFriends;

        public int UserProfileFriends
        {
            get
            { return _UserProfileFriends; }
            set
            {
                if (_UserProfileFriends == value)
                    return;
                _UserProfileFriends = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserProfileFollowers変更通知プロパティ
        private int _UserProfileFollowers;

        public int UserProfileFollowers
        {
            get
            { return _UserProfileFollowers; }
            set
            {
                if (_UserProfileFollowers == value)
                    return;
                _UserProfileFollowers = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserProfileFavorites変更通知プロパティ
        private int _UserProfileFavorites;

        public int UserProfileFavorites
        {
            get
            { return _UserProfileFavorites; }
            set
            {
                if (_UserProfileFavorites == value)
                    return;
                _UserProfileFavorites = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsTextNoInput変更通知プロパティ
        private bool _IsTextNoInput = true;

        public bool IsTextNoInput
        {
            get
            { return _IsTextNoInput; }
            set
            {
                if (_IsTextNoInput == value)
                    return;
                _IsTextNoInput = value;
                RaisePropertyChanged();
            }
        }
        #endregion


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
                IsTextNoInput = value == "";
                RaisePropertyChanged();

                RaisePropertyChanged(() => UpdateStatusTextLength);
                RaisePropertyChanged(() => IsTextNoInput);
                SayDajareCommand.RaiseCanExecuteChanged();
                UpdateStatusCommand.RaiseCanExecuteChanged();

                errors["UpdateStatusText"] = (_UpdateStatusTextLength > 140) ? "140文字を超えています" : null;
            }
        }
        #endregion


        #region UpdateStatusTextLength変更通知プロパティ
        private int _UpdateStatusTextLength = 140;

        public int UpdateStatusTextLength
        {
            get
            {
                var s = _UpdateStatusText;
                s = s.Replace("https://", " http://");
                s = Regex.Replace(s, "https?://(([\\w]|[^ -~])+(([\\w\\-]|[^ -~])+([\\w]|[^ -~]))?\\.)+(aero|asia|biz|cat|com|coop|edu|gov|info|int|jobs|mil|mobi|museum|name|net|org|pro|tel|travel|xxx|ac|ad|ae|af|ag|ai|al|am|an|ao|aq|ar|as|at|au|aw|ax|az|ba|bb|bd|be|bf|bg|bh|bi|bj|bm|bn|bo|br|bs|bt|bv|bw|by|bz|ca|cc|cd|cf|cg|ch|ci|ck|cl|cm|cn|co|cr|cs|cu|cv|cx|cy|cz|dd|de|dj|dk|dm|do|dz|ec|ee|eg|eh|er|es|et|eu|fi|fj|fk|fm|fo|fr|ga|gb|gd|ge|gf|gg|gh|gi|gl|gm|gn|gp|gq|gr|gs|gt|gu|gw|gy|hk|hm|hn|hr|ht|hu|id|ie|il|im|in|io|iq|ir|is|it|je|jm|jo|jp|ke|kg|kh|ki|km|kn|kp|kr|kw|ky|kz|la|lb|lc|li|lk|lr|ls|lt|lu|lv|ly|ma|mc|md|me|mg|mh|mk|ml|mm|mn|mo|mp|mq|mr|ms|mt|mu|mv|mw|mx|my|mz|na|nc|ne|nf|ng|ni|nl|no|np|nr|nu|nz|om|pa|pe|pf|pg|ph|pk|pl|pm|pn|pr|ps|pt|pw|py|qa|re|ro|rs|ru|rw|sa|sb|sc|sd|se|sg|sh|si|sj|sk|sl|sm|sn|so|sr|ss|st|su|sv|sy|sz|tc|td|tf|tg|th|tj|tk|tl|tm|tn|to|tp|tr|tt|tv|tw|tz|ua|ug|uk|us|uy|uz|va|vc|ve|vg|vi|vn|vu|wf|ws|ye|yt|za|zm|zw)(?![\\w])(/([\\w\\.\\-\\$&%/:=#~!]*\\??[\\w\\.\\-\\$&%/:=#~!]*[\\w\\-\\$/#])?)?", "                      ");
                s = s.Replace("\r", "");
                if (HasMedia) s += "there is a imageaddress";
                _UpdateStatusTextLength = 140 - s.Length;
                return _UpdateStatusTextLength;
            }
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
            return !_tokenus && UpdateStatusText.Length <= 140 && UpdateStatusText.Length != 0;
        }

        public async void UpdateStatus()
        {
            _tokenus = true;
            UpdateStatusCommand.RaiseCanExecuteChanged();

            Dictionary<string, object> opt = new Dictionary<string, object>();
            opt["status"] = UpdateStatusText;
            if (IsReplying) opt["in_reply_to_status_id"] = ReplyingStatus.Id;

            try
            {
                if (HasMedia)
                {
                    opt["media"] = Media;
                    await kbtter.Token.Statuses.UpdateWithMediaAsync(opt);
                    RemoveMedia();
                }
                else
                {
                    await kbtter.Token.Statuses.UpdateAsync(opt);
                }
                NotifyInformation(string.Format("ツイートを送信しました"));
            }
            catch (TwitterException e)
            {
                NotifyInformation(string.Format("ツイートの送信に失敗しました : {0}", e.Message));
            }
            catch
            { }

            _tokenus = false;
            UpdateStatusCommand.RaiseCanExecuteChanged();
            UpdateStatusText = "";
            IsReplying = false;
            ReplyingStatus = new Status();
        }
        #endregion


        #region IsReplying変更通知プロパティ
        private bool _IsReplying;

        public bool IsReplying
        {
            get
            { return _IsReplying; }
            set
            {
                if (_IsReplying == value)
                    return;
                _IsReplying = value;
                CancelReplyCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ReplyingStatus変更通知プロパティ
        private Status _ReplyingStatus = new Status { Text = "" };

        public Status ReplyingStatus
        {
            get
            { return _ReplyingStatus; }
            set
            {
                if (_ReplyingStatus == value)
                    return;
                _ReplyingStatus = value;
                ReplyingStatusText = value.Text;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ReplyingStatusText変更通知プロパティ
        private string _ReplyingStatusText = "";

        public string ReplyingStatusText
        {
            get
            { return _ReplyingStatusText; }
            set
            {
                if (_ReplyingStatusText == value)
                    return;
                _ReplyingStatusText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region HasMedia変更通知プロパティ
        public bool HasMedia
        {
            get
            { return Media != null; }
        }
        #endregion


        internal Stream Media { get; set; }


        #region MediaPath変更通知プロパティ
        private string _MediaPath = "";

        public string MediaPath
        {
            get
            { return _MediaPath; }
            set
            {
                if (_MediaPath == value)
                    return;
                _MediaPath = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region AddMediaCommand
        private ListenerCommand<OpeningFileSelectionMessage> _AddMediaCommand;

        public ListenerCommand<OpeningFileSelectionMessage> AddMediaCommand
        {
            get
            {
                if (_AddMediaCommand == null)
                {
                    _AddMediaCommand = new ListenerCommand<OpeningFileSelectionMessage>(AddMedia);
                }
                return _AddMediaCommand;
            }
        }

        public async void AddMedia(OpeningFileSelectionMessage parameter)
        {
            await Task.Run(() =>
            {
                if (parameter.Response.Length == 0) return;
                Media = File.Open(parameter.Response[0], FileMode.Open);
                MediaPath = parameter.Response[0];
                RaisePropertyChanged(() => HasMedia);
                RemoveMediaCommand.RaiseCanExecuteChanged();
            });
        }

        public async void AddMediaDirect(string filepath)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(filepath)) return;
                Media = File.Open(filepath, FileMode.Open);
                MediaPath = filepath;
                RaisePropertyChanged(() => HasMedia);
                RemoveMediaCommand.RaiseCanExecuteChanged();
            });

        }
        #endregion


        #region RemoveMediaCommand
        private ViewModelCommand _RemoveMediaCommand;

        public ViewModelCommand RemoveMediaCommand
        {
            get
            {
                if (_RemoveMediaCommand == null)
                {
                    _RemoveMediaCommand = new ViewModelCommand(RemoveMedia, CanRemoveMedia);
                }
                return _RemoveMediaCommand;
            }
        }

        public bool CanRemoveMedia()
        {
            return HasMedia;
        }

        public void RemoveMedia()
        {
            Media.Dispose();
            Media = null;
            RaisePropertyChanged(() => HasMedia);
            RaisePropertyChanged(() => UpdateStatusTextLength);
            RemoveMediaCommand.RaiseCanExecuteChanged();
        }
        #endregion


        public void SetReplyTo(Status rep)
        {
            IsReplying = true;
            ReplyingStatus = rep;

            List<string> ru = ReplyingStatus.Entities.UserMentions.Select(p => p.ScreenName).ToList();
            if (!ru.Contains(rep.User.ScreenName)) ru.Add(rep.User.ScreenName);
            if (ru.Count != 1 && ru.Contains(kbtter.AuthenticatedUser.ScreenName)) ru.Remove(kbtter.AuthenticatedUser.ScreenName);
            var t = new StringBuilder();
            ru.ForEach(p => t.Append(String.Format("@{0} ", p)));

            UpdateStatusText = t.ToString();
            RaisePropertyChanged("ReplyStart");
        }


        #region CancelReplyCommand
        private ViewModelCommand _CancelReplyCommand;

        public ViewModelCommand CancelReplyCommand
        {
            get
            {
                if (_CancelReplyCommand == null)
                {
                    _CancelReplyCommand = new ViewModelCommand(CancelReply, CanCancelReply);
                }
                return _CancelReplyCommand;
            }
        }

        public bool CanCancelReply()
        {
            return IsReplying;
        }

        public void CancelReply()
        {
            IsReplying = false;
            UpdateStatusText = "";
        }
        #endregion


        #region ToggleNewStatusCommand
        private ViewModelCommand _ToggleNewStatusCommand;

        public ViewModelCommand ToggleNewStatusCommand
        {
            get
            {
                if (_ToggleNewStatusCommand == null)
                {
                    _ToggleNewStatusCommand = new ViewModelCommand(ToggleNewStatus);
                }
                return _ToggleNewStatusCommand;
            }
        }

        public void ToggleNewStatus()
        {
            RaisePropertyChanged("ToggleNewStatus");
        }
        #endregion


        #region ApplicationExitCommand
        private ViewModelCommand _ApplicationExitCommand;

        public ViewModelCommand ApplicationExitCommand
        {
            get
            {
                if (_ApplicationExitCommand == null)
                {
                    _ApplicationExitCommand = new ViewModelCommand(ApplicationExit);
                }
                return _ApplicationExitCommand;
            }
        }

        public void ApplicationExit()
        {
            App.Current.Shutdown();
        }
        #endregion


        #region FireCSharpBeamCommand
        private ViewModelCommand _FireCSharpBeamCommand;

        public ViewModelCommand FireCSharpBeamCommand
        {
            get
            {
                if (_FireCSharpBeamCommand == null)
                {
                    _FireCSharpBeamCommand = new ViewModelCommand(FireCSharpBeam);
                }
                return _FireCSharpBeamCommand;
            }
        }

        public void FireCSharpBeam()
        {
            kbtter.FireBeam("C#");
        }
        #endregion


        #region FireXamlBeamCommand
        private ViewModelCommand _FireXamlBeamCommand;

        public ViewModelCommand FireXamlBeamCommand
        {
            get
            {
                if (_FireXamlBeamCommand == null)
                {
                    _FireXamlBeamCommand = new ViewModelCommand(FireXamlBeam);
                }
                return _FireXamlBeamCommand;
            }
        }

        public void FireXamlBeam()
        {
            kbtter.FireBeam("XAML");
        }
        #endregion


        #region FireWpfBeamCommand
        private ViewModelCommand _FireWpfBeamCommand;

        public ViewModelCommand FireWpfBeamCommand
        {
            get
            {
                if (_FireWpfBeamCommand == null)
                {
                    _FireWpfBeamCommand = new ViewModelCommand(FireWpfBeam);
                }
                return _FireWpfBeamCommand;
            }
        }

        public void FireWpfBeam()
        {
            kbtter.FireBeam("WPF");
        }
        #endregion


        #region FireCoreTweetBeamCommand
        private ViewModelCommand _FireCoreTweetBeamCommand;

        public ViewModelCommand FireCoreTweetBeamCommand
        {
            get
            {
                if (_FireCoreTweetBeamCommand == null)
                {
                    _FireCoreTweetBeamCommand = new ViewModelCommand(FireCoreTweetBeam);
                }
                return _FireCoreTweetBeamCommand;
            }
        }

        public void FireCoreTweetBeam()
        {
            kbtter.FireBeam("CoreTweet");
        }
        #endregion


        #region FireLivetBeamCommand
        private ViewModelCommand _FireLivetBeamCommand;

        public ViewModelCommand FireLivetBeamCommand
        {
            get
            {
                if (_FireLivetBeamCommand == null)
                {
                    _FireLivetBeamCommand = new ViewModelCommand(FireLivetBeam);
                }
                return _FireLivetBeamCommand;
            }
        }

        public void FireLivetBeam()
        {
            kbtter.FireBeam("Livet");
        }
        #endregion


        #region FireJsonBeamCommand
        private ViewModelCommand _FireJsonBeamCommand;

        public ViewModelCommand FireJsonBeamCommand
        {
            get
            {
                if (_FireJsonBeamCommand == null)
                {
                    _FireJsonBeamCommand = new ViewModelCommand(FireJsonBeam);
                }
                return _FireJsonBeamCommand;
            }
        }

        public void FireJsonBeam()
        {
            kbtter.FireBeam("JSON");
        }
        #endregion


        #region FireReactiveExtensionsBeamCommand
        private ViewModelCommand _FireReactiveExtensionsBeamCommand;

        public ViewModelCommand FireReactiveExtensionsBeamCommand
        {
            get
            {
                if (_FireReactiveExtensionsBeamCommand == null)
                {
                    _FireReactiveExtensionsBeamCommand = new ViewModelCommand(FireReactiveExtensionsBeam);
                }
                return _FireReactiveExtensionsBeamCommand;
            }
        }

        public void FireReactiveExtensionsBeam()
        {
            kbtter.FireBeam("Rx");
        }
        #endregion


        #region FireSQLiteBeamCommand
        private ViewModelCommand _FireSQLiteBeamCommand;

        public ViewModelCommand FireSQLiteBeamCommand
        {
            get
            {
                if (_FireSQLiteBeamCommand == null)
                {
                    _FireSQLiteBeamCommand = new ViewModelCommand(FireSQLiteBeam);
                }
                return _FireSQLiteBeamCommand;
            }
        }

        public void FireSQLiteBeam()
        {
            kbtter.FireBeam("SQLite");
        }
        #endregion


        #region SayDajareCommand
        private ViewModelCommand _SayDajareCommand;

        public ViewModelCommand SayDajareCommand
        {
            get
            {
                if (_SayDajareCommand == null)
                {
                    _SayDajareCommand = new ViewModelCommand(SayDajare, CanSayDajare);
                }
                return _SayDajareCommand;
            }
        }

        public bool CanSayDajare()
        {
            return UpdateStatusText != "";
        }

        public void SayDajare()
        {
            kbtter.SayDajare(UpdateStatusText);
            UpdateStatusText = "";
        }
        #endregion


        #region GCCollectCommand
        private ViewModelCommand _GCCollectCommand;

        public ViewModelCommand GCCollectCommand
        {
            get
            {
                if (_GCCollectCommand == null)
                {
                    _GCCollectCommand = new ViewModelCommand(GCCollect);
                }
                return _GCCollectCommand;
            }
        }

        public void GCCollect()
        {
            GC.Collect();
        }
        #endregion


        #region DirectMessages変更通知プロパティ
        private ObservableSynchronizedCollection<DirectMessageViewModel> _DirectMessages = new ObservableSynchronizedCollection<DirectMessageViewModel>();

        public ObservableSynchronizedCollection<DirectMessageViewModel> DirectMessages
        {
            get
            { return _DirectMessages; }
            set
            {
                if (_DirectMessages == value)
                    return;
                _DirectMessages = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region SendingDirectMessageTarget変更通知プロパティ
        private DirectMessageViewModel _SendingDirectMessageTarget = new DirectMessageViewModel();

        public DirectMessageViewModel SendingDirectMessageTarget
        {
            get
            { return _SendingDirectMessageTarget; }
            set
            {
                if (_SendingDirectMessageTarget == value)
                    return;
                _SendingDirectMessageTarget = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsDMTextNoInput変更通知プロパティ
        private bool _IsDMTextNoInput;

        public bool IsDMTextNoInput
        {
            get
            { return _IsDMTextNoInput; }
            set
            {
                if (_IsDMTextNoInput == value)
                    return;
                _IsDMTextNoInput = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region DirectMessageText変更通知プロパティ
        private string _DirectMessageText = "";

        public string DirectMessageText
        {
            get
            { return _DirectMessageText; }
            set
            {
                if (_DirectMessageText == value)
                    return;
                IsDMTextNoInput = value == "";
                RaisePropertyChanged(() => DirectMessageTextLength);
                SendDirectMessageCommand.RaiseCanExecuteChanged();
                _DirectMessageText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region DirectMessageTextLength変更通知プロパティ

        public int DirectMessageTextLength
        {
            get
            { return 140 - DirectMessageText.Length; }
        }
        #endregion


        #region SendDirectMessageCommand
        private ViewModelCommand _SendDirectMessageCommand;
        bool _tokensd = false;

        public ViewModelCommand SendDirectMessageCommand
        {
            get
            {
                if (_SendDirectMessageCommand == null)
                {
                    _SendDirectMessageCommand = new ViewModelCommand(SendDirectMessage, CanSendDirectMessage);
                }
                return _SendDirectMessageCommand;
            }
        }

        public bool CanSendDirectMessage()
        {
            return !_tokensd && 0 < DirectMessageTextLength && DirectMessageTextLength < 140;
        }

        public async void SendDirectMessage()
        {
            _tokenus = true;
            try
            {
                Dictionary<string, object> opt = new Dictionary<string, object>();
                opt["text"] = DirectMessageText;
                opt["screen_name"] = SendingDirectMessageTarget.TargetUserScreenName;
                await kbtter.Token.DirectMessages.NewAsync(opt);
                NotifyInformation("ダイレクトメッセージを送信しました");
            }
            catch (TwitterException e)
            {
                NotifyInformation(string.Format("ダイレクトメッセージを送信できませんでした : {0}", e.Message));
            }
            catch
            { }
            _tokenus = false;
            DirectMessageText = "";
        }
        #endregion


        #region NewDirectMessageTargetScreenName変更通知プロパティ
        private string _NewDirectMessageTargetScreenName = "";

        public string NewDirectMessageTargetScreenName
        {
            get
            { return _NewDirectMessageTargetScreenName; }
            set
            {
                if (_NewDirectMessageTargetScreenName == value)
                    return;
                _NewDirectMessageTargetScreenName = value;
                AddDirectMessageTargetCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged();
            }
        }
        #endregion


        #region AddDirectMessageTargetCommand
        private ViewModelCommand _AddDirectMessageTargetCommand;

        public ViewModelCommand AddDirectMessageTargetCommand
        {
            get
            {
                if (_AddDirectMessageTargetCommand == null)
                {
                    _AddDirectMessageTargetCommand = new ViewModelCommand(AddDirectMessageTarget, CanAddDirectMessageTarget);
                }
                return _AddDirectMessageTargetCommand;
            }
        }

        public bool CanAddDirectMessageTarget()
        {
            return NewDirectMessageTargetScreenName != "";
        }

        public async void AddDirectMessageTarget()
        {
            try
            {
                var fs = await kbtter.Token.Friendships.ShowAsync(source_id => kbtter.AuthenticatedUser.ScreenName, target_screen_name => NewDirectMessageTargetScreenName);
                if (!fs.Target.CanDM ?? false)
                {
                    NotifyInformation("そのユーザーにはダイレクトメッセージを送れません");
                    return;
                }
                else
                {
                    var user = await kbtter.Token.Users.ShowAsync(screen_name => NewDirectMessageTargetScreenName);
                    var dmvm = new DirectMessageViewModel(this, kbtter.AuthenticatedUser.ScreenName);
                    dmvm.TargetUserName = user.Name;
                    dmvm.TargetUserScreenName = user.ScreenName;
                    dmvm.TargetUserImageUri = user.ProfileImageUrlHttps;
                    DirectMessages.Add(dmvm);
                }
            }
            catch (TwitterException e)
            {
                NotifyInformation(string.Format("ユーザー情報の取得に失敗しました : {0}", e.Message));
            }
            catch
            { }
        }
        #endregion


        #region ReconnectStreamingCommand
        private ViewModelCommand _ReconnectStreamingCommand;

        public ViewModelCommand ReconnectStreamingCommand
        {
            get
            {
                if (_ReconnectStreamingCommand == null)
                {
                    _ReconnectStreamingCommand = new ViewModelCommand(ReconnectStreaming);
                }
                return _ReconnectStreamingCommand;
            }
        }

        public void ReconnectStreaming()
        {
            kbtter.RestartStreaming();
            strstop = false;
            StopStreamingCommand.RaiseCanExecuteChanged();
        }
        #endregion


        #region StopStreamingCommand
        private ViewModelCommand _StopStreamingCommand;
        bool strstop = false;

        public ViewModelCommand StopStreamingCommand
        {
            get
            {
                if (_StopStreamingCommand == null)
                {
                    _StopStreamingCommand = new ViewModelCommand(StopStreaming, CanStopStreaming);
                }
                return _StopStreamingCommand;
            }
        }

        public bool CanStopStreaming()
        {
            return !strstop;
        }

        public void StopStreaming()
        {
            kbtter.StopStreaming();
            strstop = true;
            StopStreamingCommand.RaiseCanExecuteChanged();
        }
        #endregion


        #region NotificationText変更通知プロパティ
        private string _NotificationText;

        public string NotificationText
        {
            get
            { return _NotificationText; }
            set
            {
                _NotificationText = value;
            }
        }
        #endregion


        public void NotifyInformation(string text)
        {
            NotificationText = text;
            if (!string.IsNullOrEmpty(text)) RaisePropertyChanged(() => NotificationText);
        }


        #region SearchText変更通知プロパティ
        private string _SearchText = "";

        public string SearchText
        {
            get
            { return _SearchText; }
            set
            {
                if (_SearchText == value)
                    return;
                _SearchText = value;
                RaisePropertyChanged();
                SearchStatusesCommand.RaiseCanExecuteChanged();
                SearchUserCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion


        #region SearchStatusesCommand
        private ViewModelCommand _SearchStatusesCommand;

        public ViewModelCommand SearchStatusesCommand
        {
            get
            {
                if (_SearchStatusesCommand == null)
                {
                    _SearchStatusesCommand = new ViewModelCommand(SearchStatuses, CanSearchStatuses);
                }
                return _SearchStatusesCommand;
            }
        }

        public bool CanSearchStatuses()
        {
            return !string.IsNullOrEmpty(SearchText);
        }

        public void SearchStatuses()
        {

        }
        #endregion


        #region SearchUserCommand
        private ViewModelCommand _SearchUserCommand;

        public ViewModelCommand SearchUserCommand
        {
            get
            {
                if (_SearchUserCommand == null)
                {
                    _SearchUserCommand = new ViewModelCommand(SearchUser, CanSearchUser);
                }
                return _SearchUserCommand;
            }
        }

        public bool CanSearchUser()
        {
            return !string.IsNullOrEmpty(SearchText);
        }

        public void SearchUser()
        {

        }
        #endregion


        public void RequestStatusAction(string type, string info)
        {
            StatusAction = new StatusAction { Type = type, Information = info };
        }


        #region StatusAction変更通知プロパティ
        private StatusAction _StatusAction;

        public StatusAction StatusAction
        {
            get
            { return _StatusAction; }
            set
            {
                if (_StatusAction == value)
                    return;
                _StatusAction = value;
                RaisePropertyChanged();
            }
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

        #region AddUserTimelineCommand
        private ViewModelCommand _AddUserTimelineCommand;

        public ViewModelCommand AddUserTimelineCommand
        {
            get
            {
                if (_AddUserTimelineCommand == null)
                {
                    _AddUserTimelineCommand = new ViewModelCommand(AddUserTimeline);
                }
                return _AddUserTimelineCommand;
            }
        }

        public void AddUserTimeline()
        {
            Messenger.Raise(new TransitionMessage(new NewUserTimelineViewModel(this), "NewUserTimeline"));
        }

        public UserCustomizableTimelineViewModel UserTimelineViewModel { get; private set; }
        public void RequestUserTimeline(UserCustomizableTimelineViewModel vm)
        {
            UserTimelineViewModel = vm;
            RaisePropertyChanged("UserTimeline");
        }
        #endregion


        public object ClosedTabTag { get; set; }
        public void RaiseTabClose(object tag)
        {
            ClosedTabTag = tag;
            RaisePropertyChanged("TabClose");
        }

    }

    internal delegate void StatusUpdateEventHandler(object sender, StatusViewModel vm);
    internal delegate void EventUpdateEventHandler(object sender, NotificationViewModel vm);

    internal sealed class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        Func<T, T, bool> pred;

        public GenericEqualityComparer(Func<T, T, bool> pr)
        {
            pred = pr;
        }
        public bool Equals(T x, T y)
        {
            return pred(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }

    internal class StatusAction
    {
        public string Type { get; set; }
        public string Information { get; set; }

        public StatusAction()
        {
            Type = "";
            Information = "";
        }
    }

}
