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
using StatusElement = Kbtter3.ViewModels.StatusViewModel.StatusElement;
using EntityInfo = Kbtter3.ViewModels.StatusViewModel.EntityInfo;

namespace Kbtter3.ViewModels
{
    internal class DirectMessageItemViewModel : ViewModel
    {

        DirectMessage dirmes;

        public DirectMessageItemViewModel()
        {

        }

        public DirectMessageItemViewModel(DirectMessage dm)
        {
            dirmes = dm;
            Initialize();
        }

        public void Initialize()
        {
            Text = dirmes.Text;
            UserName = dirmes.Sender.Name;
            ScreenName = dirmes.Sender.ScreenName;
            UserImageUri = dirmes.Sender.ProfileImageUrlHttps;
            CreatedTimeText = dirmes.CreatedAt.LocalDateTime.ToString();
            AnalyzeText();
        }

        internal void AnalyzeText()
        {
            _Text = _Text
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");
            TextElements = new List<StatusElement>();

            var el = new List<EntityInfo>();
            if (dirmes.Entities.Urls != null) el.AddRange(dirmes.Entities.Urls.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Url" }));
            if (dirmes.Entities.Media != null) el.AddRange(dirmes.Entities.Media.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Media" }));
            if (dirmes.Entities.UserMentions != null) el.AddRange(dirmes.Entities.UserMentions.Select(p => new EntityInfo { Indices = p.Indices, Text = "@" + p.ScreenName, Infomation = p.ScreenName, Type = "Mention" }));
            if (dirmes.Entities.HashTags != null) el.AddRange(dirmes.Entities.HashTags.Select(p => new EntityInfo { Indices = p.Indices, Text = "#" + p.Text, Infomation = p.Text, Type = "Hashtag" }));
            el.Sort((x, y) => x.Indices[0].CompareTo(y.Indices[0]));
            int n = 0;
            string s = _Text;
            foreach (var i in el)
            {
                TextElements.Add(new StatusElement { Text = s.Substring(n, i.Indices[0] - n), Type = "None" });
                TextElements.Add(new StatusElement { Text = i.Text, Infomation = i.Infomation, Type = i.Type });
                n = i.Indices[1];
            }
            if (n < s.Length) TextElements.Add(new StatusElement { Text = s.Substring(n), Type = "None" });
        }

        public IList<StatusElement> TextElements { get; set; }

        #region UserImageUri変更通知プロパティ
        private Uri _UserImageUri;

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


        #region UserName変更通知プロパティ
        private string _UserName = "UserName";

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
        private string _ScreenName = "ScreenName";

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
        private string _Text = "Text";

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


        #region DecodedText変更通知プロパティ
        private string _DecodedText;

        public string DecodedText
        {
            get
            { return _DecodedText; }
            set
            {
                if (_DecodedText == value)
                    return;
                _DecodedText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region CreatedTimeText変更通知プロパティ
        private string _CreatedTimeText = "Date-Time";

        public string CreatedTimeText
        {
            get
            { return _CreatedTimeText; }
            set
            {
                if (_CreatedTimeText == value)
                    return;
                _CreatedTimeText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsReceived変更通知プロパティ
        private bool _IsReceived;

        public bool IsReceived
        {
            get
            { return _IsReceived; }
            set
            { 
                if (_IsReceived == value)
                    return;
                _IsReceived = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsSent変更通知プロパティ
        private bool _IsSent;

        public bool IsSent
        {
            get
            { return _IsSent; }
            set
            { 
                if (_IsSent == value)
                    return;
                _IsSent = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}
