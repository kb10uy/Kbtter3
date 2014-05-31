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
    internal class UserProfilePageViewModel : ViewModel
    {
        User user;
        public UserProfilePageViewModel()
        {

        }

        public UserProfilePageViewModel(User us)
        {
            user = us;
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
            Url = user.Url ?? new Uri("",UriKind.RelativeOrAbsolute);
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


    }
}
