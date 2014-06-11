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
    internal class SimpleUserViewModel : ViewModel
    {
        User user;
        public SimpleUserViewModel()
        {
            user = new User();
        }

        public SimpleUserViewModel(User u)
        {
            user = u;
        }

        public void Initialize()
        {
            Name = user.Name;
            ScreenName = user.ScreenName;
            ProfileImageUri = user.ProfileImageUrlHttps;
        }


        #region Name変更通知プロパティ
        private string _Name = "";

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
        private string _ScreenName = "";

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


        #region ProfileImageUri変更通知プロパティ
        private Uri _ProfileImageUri = new Uri("", UriKind.RelativeOrAbsolute);

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


        #region FriendshipStateText変更通知プロパティ
        private string _FriendshipStateText="フォローしていません";

        public string FriendshipStateText
        {
            get
            { return _FriendshipStateText; }
            set
            { 
                if (_FriendshipStateText == value)
                    return;
                _FriendshipStateText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


    }
}
