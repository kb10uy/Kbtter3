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
            ret._IsFavorited = (st.IsFavorited ?? false) || Kbtter.Instance.IsFavoritedInCache(st);
            ret._IsRetweeted = (st.IsRetweeted ?? false) || (ret.origin.RetweetedStatus != null && Kbtter.Instance.IsRetweetedInCache(ret.origin));
            ret._FavoriteCount = st.FavoriteCount ?? 0;
            ret.AnalyzeText();
            ret.TryGetReply();

            var vm = reg.Match(st.Source);
            ret._Via = vm.Groups["client"].Value;
            if (vm.Groups["url"].Value != "") ret._ViaUri = new Uri(vm.Groups["url"].Value);

            return ret;
        }

        public void Initialize()
        {
        }

        private void TryGetReply()
        {
            if (status.InReplyToStatusId == null) return;
            long id = status.InReplyToStatusId ?? 0;
            var rs = kbtter.Cache.Where(p => p.Id == id).FirstOrDefault();
            if (rs != null)
            {
                _ReplyUserName = rs.User.Name;
                _ReplyText = rs.Text.Replace("\r", "")
                                    .Replace("\n", "");
                _IsReply = true;
            }
        }

        private void AnalyzeText()
        {
            _Text = _Text
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");
            TextElements = new List<StatusElement>();

            var el = new List<EntityInfo>();
            if (status.Entities.Urls != null) el.AddRange(status.Entities.Urls.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Url" }));
            if (status.Entities.Media != null) el.AddRange(status.Entities.Media.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Media" }));
            if (status.Entities.UserMentions != null) el.AddRange(status.Entities.UserMentions.Select(p => new EntityInfo { Indices = p.Indices, Text = "@" + p.ScreenName, Infomation = p.ScreenName, Type = "Mention" }));
            if (status.Entities.HashTags != null) el.AddRange(status.Entities.HashTags.Select(p => new EntityInfo { Indices = p.Indices, Text = "#" + p.Text, Infomation = p.Text, Type = "Hashtag" }));
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

        public class EntityInfo
        {
            public int[] Indices { get; set; }
            public string Text { get; set; }
            public string Infomation { get; set; }
            public string Type { get; set; }
        }

        public class StatusElement
        {
            public string Text { get; set; }
            public string Infomation { get; set; }
            public string Type { get; set; }
        }

        public IList<StatusElement> TextElements { get; private set; }


        #region ReplyUserName変更通知プロパティ
        private string _ReplyUserName = "";

        public string ReplyUserName
        {
            get
            { return _ReplyUserName; }
            set
            {
                if (_ReplyUserName == value)
                    return;
                _ReplyUserName = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ReplyText変更通知プロパティ
        private string _ReplyText = "";

        public string ReplyText
        {
            get
            { return _ReplyText; }
            set
            {
                if (_ReplyText == value)
                    return;
                _ReplyText = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region IsReply変更通知プロパティ
        private bool _IsReply;

        public bool IsReply
        {
            get
            { return _IsReply; }
            set
            {
                if (_IsReply == value)
                    return;
                _IsReply = value;
                RaisePropertyChanged();
            }
        }
        #endregion


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
                    rtid = kbtter.Token.Statuses.Retweet(id => status.Id).Id;
                }
                else
                {
                    if (rtid == 0)
                    {
                        rtid = kbtter.Token.Statuses.Show(id => status.Id, include_my_retweet => true).CurrentUserRetweet ?? 0;
                    }
                    kbtter.Token.Statuses.Destroy(id => rtid);
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
                    try
                    {
                        kbtter.Token.Favorites.Create(id => origin.Id);
                    }
                    catch (TwitterException e)
                    {

                    }

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
