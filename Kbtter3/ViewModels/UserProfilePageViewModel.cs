using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

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
    internal class UserProfilePageViewModel : ViewModel
    {
        User user;
        MainWindowViewModel mainw;
        Kbtter kbtter = Kbtter.Instance;
        public UserProfilePageViewModel()
        {

        }

        public UserProfilePageViewModel(User us, MainWindowViewModel vm)
        {
            user = us;
            mainw = vm;
        }

        public void Initialize()
        {
            BannerImageUri = user.ProfileBannerUrl;
            UserImageUri = user.ProfileImageUrlHttps;
            Name = user.Name;
            ScreenName = "@" + user.ScreenName;
            TextColor = "#" + user.ProfileTextColor;
            Description = user.Description;
            Location = user.Location;
            Url = user.Url ?? new Uri("", UriKind.RelativeOrAbsolute);
            Statuses = user.StatusesCount;
            Favorites = user.FavouritesCount;
            Friends = user.FriendsCount;
            Followers = user.FollowersCount;

        }


        #region BannerImageUri変更通知プロパティ
        private Uri _BannerImageUri = new Uri("/Resources/cancel.png", UriKind.Relative);

        public Uri BannerImageUri
        {
            get
            { return _BannerImageUri; }
            set
            {
                if (_BannerImageUri == value)
                    return;
                _BannerImageUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserImageUri変更通知プロパティ
        private Uri _UserImageUri = new Uri("/Resources/cancel.png", UriKind.Relative);

        public Uri UserImageUri
        {
            get
            { return _UserImageUri; }
            set
            {
                if (_UserImageUri == value)
                    return;
                _UserImageUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Name変更通知プロパティ
        private string _Name = "Name";

        public string Name
        {
            get
            { return _Name; }
            set
            {
                if (_Name == value)
                    return;
                _Name = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ScreenName変更通知プロパティ
        private string _ScreenName = "UserName";

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


        #region TextColor変更通知プロパティ
        private string _TextColor = "#000000";

        public string TextColor
        {
            get
            { return _TextColor; }
            set
            {
                if (_TextColor == value)
                    return;
                _TextColor = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Description変更通知プロパティ
        private string _Description = "ユーザーbio表示領域";

        public string Description
        {
            get
            { return _Description; }
            set
            {
                if (_Description == value)
                    return;
                _Description = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Location変更通知プロパティ
        private string _Location = "Location";

        public string Location
        {
            get
            { return _Location; }
            set
            {
                if (_Location == value)
                    return;
                _Location = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Url変更通知プロパティ
        private Uri _Url;

        public Uri Url
        {
            get
            { return _Url; }
            set
            {
                if (_Url == value)
                    return;
                _Url = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Statuses変更通知プロパティ
        private int _Statuses;

        public int Statuses
        {
            get
            { return _Statuses; }
            set
            {
                if (_Statuses == value)
                    return;
                _Statuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Favorites変更通知プロパティ
        private int _Favorites;

        public int Favorites
        {
            get
            { return _Favorites; }
            set
            {
                if (_Favorites == value)
                    return;
                _Favorites = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Friends変更通知プロパティ
        private int _Friends;

        public int Friends
        {
            get
            { return _Friends; }
            set
            {
                if (_Friends == value)
                    return;
                _Friends = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region Followers変更通知プロパティ
        private int _Followers;

        public int Followers
        {
            get
            { return _Followers; }
            set
            {
                if (_Followers == value)
                    return;
                _Followers = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ShowingStatuses変更通知プロパティ
        private ObservableCollection<StatusViewModel> _ShowingStatuses = new ObservableCollection<StatusViewModel>();

        public ObservableCollection<StatusViewModel> ShowingStatuses
        {
            get
            { return _ShowingStatuses; }
            set
            {
                if (_ShowingStatuses == value)
                    return;
                _ShowingStatuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ShowOlderStatusesCommand
        private ViewModelCommand _ShowOlderStatusesCommand;
        long stoc = 0;

        public ViewModelCommand ShowOlderStatusesCommand
        {
            get
            {
                if (_ShowOlderStatusesCommand == null)
                {
                    _ShowOlderStatusesCommand = new ViewModelCommand(ShowOlderStatuses, CanShowOlderStatuses);
                }
                return _ShowOlderStatusesCommand;
            }
        }

        public bool CanShowOlderStatuses()
        {
            return stoc != 0;
        }

        public async void ShowOlderStatuses()
        {
            ShowingStatuses.Clear();
            try
            {
                var s = await kbtter.Token.Statuses.UserTimelineAsync(screen_name => user.ScreenName, count => showst, max_id => stoc);
                stoc = s.Last().Id;
                stnc = s.First().Id;
                ShowOlderStatusesCommand.RaiseCanExecuteChanged();
                ShowNewerStatusesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingStatuses.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
            }
        }
        #endregion


        #region ShowNewerStatusesCommand
        private ViewModelCommand _ShowNewerStatusesCommand;
        long stnc = 0;

        public ViewModelCommand ShowNewerStatusesCommand
        {
            get
            {
                if (_ShowNewerStatusesCommand == null)
                {
                    _ShowNewerStatusesCommand = new ViewModelCommand(ShowNewerStatuses, CanShowNewerStatuses);
                }
                return _ShowNewerStatusesCommand;
            }
        }

        public bool CanShowNewerStatuses()
        {
            return stnc != 0;
        }

        public async void ShowNewerStatuses()
        {
            ShowingStatuses.Clear();
            try
            {
                var s = await kbtter.Token.Statuses.UserTimelineAsync(screen_name => user.ScreenName, count => showst, since_id => stnc);
                stoc = s.Last().Id;
                stnc = s.First().Id;
                ShowOlderStatusesCommand.RaiseCanExecuteChanged();
                ShowNewerStatusesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingStatuses.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
            }
        }
        #endregion


        #region RefreshStatusesCommand
        private ViewModelCommand _RefreshStatusesCommand;
        long showst = 20;

        public ViewModelCommand RefreshStatusesCommand
        {
            get
            {
                if (_RefreshStatusesCommand == null)
                {
                    _RefreshStatusesCommand = new ViewModelCommand(RefreshStatuses);
                }
                return _RefreshStatusesCommand;
            }
        }

        public async void RefreshStatuses()
        {
            ShowingStatuses.Clear();
            try
            {
                var s = await kbtter.Token.Statuses.UserTimelineAsync(screen_name => user.ScreenName, count => showst);
                stoc = s.Last().Id;
                stnc = s.First().Id;
                ShowOlderStatusesCommand.RaiseCanExecuteChanged();
                ShowNewerStatusesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingStatuses.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
                stoc = 0;
                stnc = 0;
            }
        }
        #endregion


        #region ShowingFavorites変更通知プロパティ
        private ObservableCollection<StatusViewModel> _ShowingFavorites = new ObservableCollection<StatusViewModel>();

        public ObservableCollection<StatusViewModel> ShowingFavorites
        {
            get
            { return _ShowingFavorites; }
            set
            {
                if (_ShowingFavorites == value)
                    return;
                _ShowingFavorites = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ShowOlderFavoritesCommand
        private ViewModelCommand _ShowOlderFavoritesCommand;
        long favoc = 0;

        public ViewModelCommand ShowOlderFavoritesCommand
        {
            get
            {
                if (_ShowOlderFavoritesCommand == null)
                {
                    _ShowOlderFavoritesCommand = new ViewModelCommand(ShowOlderFavorites, CanShowOlderFavorites);
                }
                return _ShowOlderFavoritesCommand;
            }
        }

        public bool CanShowOlderFavorites()
        {
            return favoc != 0;
        }

        public async void ShowOlderFavorites()
        {
            ShowingFavorites.Clear();
            try
            {
                var s = await kbtter.Token.Favorites.ListAsync(screen_name => user.ScreenName, count => showst, since_id => favoc);
                favoc = s.Last().Id;
                favnc = s.First().Id;
                ShowOlderFavoritesCommand.RaiseCanExecuteChanged();
                ShowNewerFavoritesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingFavorites.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
            }
        }
        #endregion


        #region ShowNewerFavoritesCommand
        private ViewModelCommand _ShowNewerFavoritesCommand;
        long favnc = 0;

        public ViewModelCommand ShowNewerFavoritesCommand
        {
            get
            {
                if (_ShowNewerFavoritesCommand == null)
                {
                    _ShowNewerFavoritesCommand = new ViewModelCommand(ShowNewerFavorites, CanShowNewerFavorites);
                }
                return _ShowNewerFavoritesCommand;
            }
        }

        public bool CanShowNewerFavorites()
        {
            return favoc != 0;
        }

        public async void ShowNewerFavorites()
        {
            ShowingFavorites.Clear();
            try
            {
                var s = await kbtter.Token.Favorites.ListAsync(screen_name => user.ScreenName, count => showst, max_id => favnc);
                favoc = s.Last().Id;
                favnc = s.First().Id;
                ShowOlderFavoritesCommand.RaiseCanExecuteChanged();
                ShowNewerFavoritesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingFavorites.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
            }
        }
        #endregion


        #region RefreshFavoritesCommand
        private ViewModelCommand _RefreshFavoritesCommand;

        public ViewModelCommand RefreshFavoritesCommand
        {
            get
            {
                if (_RefreshFavoritesCommand == null)
                {
                    _RefreshFavoritesCommand = new ViewModelCommand(RefreshFavorites);
                }
                return _RefreshFavoritesCommand;
            }
        }

        public async void RefreshFavorites()
        {
            ShowingFavorites.Clear();
            try
            {
                var s = await kbtter.Token.Favorites.ListAsync(screen_name => user.ScreenName, count => showst);
                stoc = s.Last().Id;
                stnc = s.First().Id;
                ShowOlderFavoritesCommand.RaiseCanExecuteChanged();
                ShowNewerFavoritesCommand.RaiseCanExecuteChanged();
                foreach (var i in s)
                {
                    ShowingFavorites.Add(StatusViewModelExtension.CreateStatusViewModel(mainw, i));
                }
            }
            catch
            {
                stoc = 0;
                stnc = 0;
            }
        }
        #endregion


        #region ShowingFriends変更通知プロパティ
        private ObservableCollection<User> _ShowingFriends = new ObservableCollection<User>();

        public ObservableCollection<User> ShowingFriends
        {
            get
            { return _ShowingFriends; }
            set
            {
                if (_ShowingFriends == value)
                    return;
                _ShowingFriends = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ShowingFollowers変更通知プロパティ
        private ObservableCollection<User> _ShowingFollowers = new ObservableCollection<User>();

        public ObservableCollection<User> ShowingFollowers
        {
            get
            { return _ShowingFollowers; }
            set
            {
                if (_ShowingFollowers == value)
                    return;
                _ShowingFollowers = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}
