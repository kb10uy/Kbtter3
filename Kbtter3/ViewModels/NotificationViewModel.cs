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
using CoreTweet.Streaming;

namespace Kbtter3.ViewModels
{
    public class NotificationViewModel : ViewModel
    {
        public NotificationViewModel()
        {

        }

        public NotificationViewModel(EventMessage msg)
        {
            NotificationTime = msg.CreatedAt.LocalDateTime.ToShortTimeString();
            UserImageUri = msg.Source.ProfileImageUrlHttps;
            switch (msg.Event)
            {
                case EventCode.Favorite:
                    IconUri = new Uri("/Resources/fav.png", UriKind.Relative);
                    Description = String.Format("あなたのツイートが{0}さんのお気に入りに登録されました", msg.Source.Name);
                    ContentText = msg.TargetStatus.Text;
                    break;
                case EventCode.Unfavorite:
                    IconUri = new Uri("/Resources/favno.png", UriKind.Relative);
                    Description = String.Format("あなたのツイートが{0}さんのお気に入りから外されました", msg.Source.Name);
                    ContentText = msg.TargetStatus.Text;
                    break;
                case EventCode.Follow:
                    IconUri = new Uri("/Resources/user.png", UriKind.Relative);
                    Description = String.Format("{0}さんにフォローされました", msg.Source.Name);
                    ContentText = msg.Source.Description;
                    break;
                case EventCode.Unfollow:
                    IconUri = new Uri("/Resources/userno.png", UriKind.Relative);
                    Description = String.Format("{0}さんからフォローを外されました", msg.Source.Name);
                    ContentText = msg.Source.Description;
                    break;
                case EventCode.ListMemberAdded:
                    IconUri = new Uri("/Resources/list.png", UriKind.Relative);
                    Description = String.Format("{0}さんのリスト\"{1}\"に追加されました", msg.Source.Name, msg.TargetList.Name);
                    ContentText = msg.TargetList.Description;
                    break;
                case EventCode.ListMemberRemoved:
                    IconUri = new Uri("/Resources/listno.png", UriKind.Relative);
                    Description = String.Format("{0}さんのリスト\"{1}\"にから外されました", msg.Source.Name, msg.TargetList.Name);
                    ContentText = msg.TargetList.Description;
                    break;
                case EventCode.Block:
                    IconUri = new Uri("/Resources/block.png", UriKind.Relative);
                    Description = String.Format("{0}さんにブロックされました", msg.Source.Name);
                    ContentText = msg.Source.Description;
                    break;
                default:
                    break;
            }
        }

        public void Initialize()
        {
        }


        #region ContentText変更通知プロパティ
        private string _ContentText = "";

        public string ContentText
        {
            get
            { return _ContentText; }
            set
            {
                if (_ContentText == value)
                    return;
                _ContentText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region NotificationTime変更通知プロパティ
        private string _NotificationTime = "";

        public string NotificationTime
        {
            get
            { return _NotificationTime; }
            set
            {
                if (_NotificationTime == value)
                    return;
                _NotificationTime = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region UserImageUri変更通知プロパティ
        private Uri _UserImageUri = new Uri("Resources/cancel.png", UriKind.Relative);

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


        #region Description変更通知プロパティ
        private string _Description = "未対応の通知内容です";

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


        #region IconUri変更通知プロパティ
        private Uri _IconUri = new Uri("Resources/cancel.png", UriKind.Relative);

        public Uri IconUri
        {
            get
            { return _IconUri; }
            set
            {
                if (_IconUri == value)
                    return;
                _IconUri = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}
