﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public event StatusUpdateEventHandler StatusUpdate;
        public event EventUpdateEventHandler EventUpdate;
        Dictionary<string, string> errors = new Dictionary<string, string>();

        public void Initialize()
        {
            kbtter = Kbtter.Instance;
            listener = new PropertyChangedEventListener(kbtter);
            CompositeDisposable.Add(listener);
            RegisterHandlers();

            kbtter.Initialize();
        }

        public void RegisterHandlers()
        {
            listener.Add("AccessTokenRequest", OnAccessTokenRequest);
            listener.Add("Status", OnStatusUpdate);
            listener.Add("Event", OnEvent);
            listener.Add("AuthenticatedUser", OnUserProfileUpdate);
        }

        public void OnAccessTokenRequest(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("AccessTokenRequest");
        }

        public void OnStatusUpdate(object sender, PropertyChangedEventArgs e)
        {
            var st = kbtter.ShowingStatuses.Dequeue();
            if (StatusUpdate != null) StatusUpdate(this, this.CreateStatusViewModel(st));
            if (st.RetweetedStatus != null)
            {
                if (st.RetweetedStatus.User.Id != kbtter.AuthenticatedUser.Id) return;
                var vm = new NotificationViewModel(st);
                if (EventUpdate != null) EventUpdate(this, vm);
                return;
            }
            if (st.Entities.UserMentions.Count(p => p.ScreenName == kbtter.AuthenticatedUser.ScreenName) != 0)
            {
                var vm = new NotificationViewModel(st, this);
                if (EventUpdate != null) EventUpdate(this, vm);
            }
        }

        public void OnEvent(object sender, PropertyChangedEventArgs e)
        {
            if (kbtter.LatestEvent.Target.Id != kbtter.AuthenticatedUser.Id) return;
            var vm = new NotificationViewModel(kbtter.LatestEvent);
            if (EventUpdate != null) EventUpdate(this, vm);
        }

        public void OnUserProfileUpdate(object sender, PropertyChangedEventArgs e)
        {
            UserProfileImageUri = kbtter.AuthenticatedUser.ProfileImageUrlHttps;
            UserProfileStatuses = kbtter.AuthenticatedUser.StatusesCount;
            UserProfileFriends = kbtter.AuthenticatedUser.FriendsCount;
            UserProfileFollowers = kbtter.AuthenticatedUser.FollowersCount;
            UserProfileFavorites = kbtter.AuthenticatedUser.FavouritesCount;
        }

        public async Task<UserProfilePageViewModel> GetUserProfile(string sn)
        {
            var t = await kbtter.GetUser(sn);
            if (t == null)
            {
                return null;
            }
            return new UserProfilePageViewModel(t);
        }


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
            return !_tokenus && UpdateStatusText.Length <= 140 && UpdateStatusText.Length != 0;
        }

        public async void UpdateStatus()
        {
            _tokenus = true;
            UpdateStatusCommand.RaiseCanExecuteChanged();

            Dictionary<string, object> opt = new Dictionary<string, object>();
            opt["status"] = UpdateStatusText;
            if (IsReplying) opt["in_reply_to_status_id"] = ReplyingStatus.Id;

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
                Media = File.Open(parameter.Response[0], FileMode.Open);
                MediaPath = parameter.Response[0];
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
            RemoveMediaCommand.RaiseCanExecuteChanged();
        }
        #endregion


        public void SetReplyTo(Status rep)
        {
            IsReplying = true;
            ReplyingStatus = rep;

            List<string> ru = ReplyingStatus.Entities.UserMentions.Select(p => p.ScreenName).ToList();
            if (!ru.Contains(rep.User.ScreenName)) ru.Add(rep.User.ScreenName);
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

    internal delegate void StatusUpdateEventHandler(object sender, StatusViewModel vm);
    internal delegate void EventUpdateEventHandler(object sender, NotificationViewModel vm);

}
