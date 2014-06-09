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

using System.Threading.Tasks;
using Kbtter3.Models;
using CoreTweet;

namespace Kbtter3.ViewModels
{
    internal class StatusViewModel : ViewModel
    {
        Kbtter kbtter = Kbtter.Instance;
        internal Status origin, status;
        internal MainWindowViewModel main;
        internal PropertyChangedEventListener listener;
        static Kbtter3Setting setting;

        long rtid;

        static StatusViewModel()
        {
            setting = Kbtter3Extension.LoadJson<Kbtter3Setting>(App.ConfigurationFileName);
        }

        public void Initialize()
        {
            listener = new PropertyChangedEventListener(kbtter);
            CompositeDisposable.Add(listener);
        }

        public StatusViewModel()
        {
            Console.WriteLine();
        }

        internal void UpdateTime(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("CreatedTimeText");
        }

        internal void TryGetReply()
        {
            if (origin.InReplyToStatusId == null) return;
            long id = origin.InReplyToStatusId ?? 0;
            var rs = kbtter.Cache.Where(p => p.Id == id).FirstOrDefault();
            if (rs != null)
            {
                _ReplyUserName = rs.User.Name;
                _ReplyText = rs.Text.Replace("\r", "")
                                    .Replace("\n", "");
                _IsReply = true;
            }
        }

        internal void AnalyzeText()
        {
            _Text = _Text
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");
            TextElements = new List<StatusElement>();

            var el = new List<EntityInfo>();
            if (origin.Entities.Urls != null) el.AddRange(origin.Entities.Urls.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Url" }));
            if (origin.Entities.Media != null) el.AddRange(origin.Entities.Media.Select(p => new EntityInfo { Indices = p.Indices, Text = p.DisplayUrl, Infomation = p.ExpandedUrl.ToString(), Type = "Media" }));
            if (origin.Entities.UserMentions != null) el.AddRange(origin.Entities.UserMentions.Select(p => new EntityInfo { Indices = p.Indices, Text = "@" + p.ScreenName, Infomation = p.ScreenName, Type = "Mention" }));
            if (origin.Entities.HashTags != null) el.AddRange(origin.Entities.HashTags.Select(p => new EntityInfo { Indices = p.Indices, Text = "#" + p.Text, Infomation = p.Text, Type = "Hashtag" }));
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


        #region DeleteStatusCommand
        private ViewModelCommand _DeleteStatusCommand;

        public ViewModelCommand DeleteStatusCommand
        {
            get
            {
                if (_DeleteStatusCommand == null)
                {
                    _DeleteStatusCommand = new ViewModelCommand(DeleteStatus, CanDeleteStatus);
                }
                return _DeleteStatusCommand;
            }
        }

        public bool CanDeleteStatus()
        {
            return IsMyStatus;
        }

        public async void DeleteStatus()
        {
            await kbtter.Token.Statuses.DestroyAsync(id => status.Id);
            RaisePropertyChanged("Delete");
        }
        #endregion


        #region IsMyStatus変更通知プロパティ
        private bool _IsMyStatus;

        public bool IsMyStatus
        {
            get
            { return _IsMyStatus; }
            set
            {
                if (_IsMyStatus == value)
                    return;
                _IsMyStatus = value;
                RaisePropertyChanged();
                DeleteStatusCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion


        #region IsOthersStatus変更通知プロパティ
        private bool _IsOthersStatus;

        public bool IsOthersStatus
        {
            get
            { return _IsOthersStatus; }
            set
            {
                if (_IsOthersStatus == value)
                    return;
                _IsOthersStatus = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region ReplyToCommand
        private ViewModelCommand _ReplyToCommand;

        public ViewModelCommand ReplyToCommand
        {
            get
            {
                if (_ReplyToCommand == null)
                {
                    _ReplyToCommand = new ViewModelCommand(ReplyTo);
                }
                return _ReplyToCommand;
            }
        }

        public void ReplyTo()
        {
            main.SetReplyTo(origin);
        }
        #endregion


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
                    try
                    {
                        kbtter.Token.Statuses.RetweetAsync(id => status.Id)
                            .ContinueWith(p =>
                        {
                            if (p.IsFaulted) return;
                            rtid = p.Result.Id;
                            _IsRetweeted = value;
                        });
                    }
                    catch /*(TwitterException e)*/
                    {

                    }

                }
                else
                {
                    try
                    {
                        if (rtid == 0)
                        {
                            rtid = kbtter.Token.Statuses.Show(id => origin.Id, include_my_retweet => true).CurrentUserRetweet ?? 0;
                        }
                        kbtter.Token.Statuses.Destroy(id => rtid);

                    }
                    finally
                    {
                        _IsRetweeted = value;
                    }
                }
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
                        kbtter.Token.Favorites.CreateAsync(id => status.Id);
                        _IsFavorited = value;
                    }
                    catch /*(TwitterException e)*/
                    { }
                }
                else
                {
                    try
                    {
                        kbtter.Token.Favorites.DestroyAsync(id => status.Id);
                    }
                    finally
                    {
                        _IsFavorited = value;
                    }
                }
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
        private string _ScreenName = "screen_name";

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


        #region UserProfileImageUri変更通知プロパティ
        private Uri _UserProfileImageUri = new Uri("/Resources/cancel.png", UriKind.Relative);

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


        #region FavoriteCount変更通知プロパティ
        private long _FavoriteCount;

        public long FavoriteCount
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
        private long _RetweetCount;

        public long RetweetCount
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


        #region CreatedTimeText変更通知プロパティ
        internal DateTime _CreatedTimeText;

        public string CreatedTimeText
        {
            get
            {
                if (setting.StatusPage.ShowAbsoluteTime) return _CreatedTimeText.ToString();

                var ts = (DateTime.Now - _CreatedTimeText);
                if (ts.Days >= 10)
                {
                    return _CreatedTimeText.ToString();
                }
                else if (ts.Days >= 1)
                {
                    return String.Format("{0}日前", ts.Days);
                }
                else if (ts.Hours >= 1)
                {
                    return String.Format("{0}時間前", ts.Hours);
                }
                else if (ts.Minutes >= 1)
                {
                    return String.Format("{0}分前", ts.Minutes);
                }
                else if (ts.Seconds >= 10)
                {
                    return String.Format("{0}秒前", ts.Seconds);
                }
                else
                {
                    return "今";
                }
            }
        }
        #endregion


        #region IsReplyToMe変更通知プロパティ
        private bool _IsReplyToMe;

        public bool IsReplyToMe
        {
            get
            { return _IsReplyToMe; }
            set
            {
                if (_IsReplyToMe == value)
                    return;
                _IsReplyToMe = value;
                RaisePropertyChanged("IsReplyToMe");
            }
        }
        #endregion


        #region IsOthersRetweet変更通知プロパティ
        private bool _IsOthersRetweet;

        public bool IsOthersRetweet
        {
            get
            { return _IsOthersRetweet; }
            set
            {
                if (_IsOthersRetweet == value)
                    return;
                _IsOthersRetweet = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region WhatDoYouSayCommand
        private ViewModelCommand _WhatDoYouSayCommand;

        public ViewModelCommand WhatDoYouSayCommand
        {
            get
            {
                if (_WhatDoYouSayCommand == null)
                {
                    _WhatDoYouSayCommand = new ViewModelCommand(WhatDoYouSay);
                }
                return _WhatDoYouSayCommand;
            }
        }

        public void WhatDoYouSay()
        {
            kbtter.Token.Statuses.UpdateAsync(status => "なーにが" + Text + "じゃ");
        }
        #endregion


        #region WhoAreYouMeaningCommand
        private ViewModelCommand _WhoAreYouMeaningCommand;

        public ViewModelCommand WhoAreYouMeaningCommand
        {
            get
            {
                if (_WhoAreYouMeaningCommand == null)
                {
                    _WhoAreYouMeaningCommand = new ViewModelCommand(WhoAreYouMeaning);
                }
                return _WhoAreYouMeaningCommand;
            }
        }

        public void WhoAreYouMeaning()
        {
            kbtter.Token.Statuses.UpdateAsync(status => "だーれが" + Text + "じゃ");
        }
        #endregion


    }

    internal static class StatusViewModelExtension
    {
        static Regex reg = new Regex("<a href=\"(?<url>.+)\" rel=\"nofollow\">(?<client>.+)</a>");

        public static StatusViewModel CreateStatusViewModel(this MainWindowViewModel vm, Status st)
        {
            var ret = new StatusViewModel();
            ret.status = st;
            if (st.RetweetedStatus != null)
            {
                ret.RetweetUserName = st.User.Name;
                st = st.RetweetedStatus;
                ret.IsRetweet = true;
            }
            else
            {
                ret.IsRetweet = false;
                ret.RetweetUserName = "";
            }
            ret.origin = st;
            ret.UserName = st.User.Name;
            ret.ScreenName = st.User.ScreenName;
            ret.Text = st.Text;
            ret.UserProfileImageUri = st.User.ProfileImageUrlHttps;
            ret.RetweetCount = st.RetweetCount ?? 0;
            ret.IsFavorited = (st.IsFavorited ?? false) || Kbtter.Instance.IsFavoritedInCache(st);
            ret.IsRetweeted = (st.IsRetweeted ?? false) || (ret.status.RetweetedStatus != null && Kbtter.Instance.IsRetweetedInCache(ret.status));
            ret.FavoriteCount = st.FavoriteCount ?? 0;
            ret.IsMyStatus = (Kbtter.Instance.AuthenticatedUser != null && Kbtter.Instance.AuthenticatedUser.Id == st.User.Id);
            ret.IsOthersStatus = !ret.IsMyStatus;
            ret.IsOthersStatus = !ret.IsMyStatus;
            ret._CreatedTimeText = st.CreatedAt.DateTime.ToLocalTime();
            ret.IsReplyToMe = st.Entities.UserMentions.Any(p => p.ScreenName == Kbtter.Instance.AuthenticatedUser.ScreenName);
            ret.AnalyzeText();
            ret.TryGetReply();

            var m = reg.Match(st.Source);
            ret.Via = m.Groups["client"].Value;
            if (m.Groups["url"].Value != "") ret.ViaUri = new Uri(m.Groups["url"].Value);

            ret.main = vm;

            ret.listener = new PropertyChangedEventListener(Kbtter.Instance);
            ret.listener.Add("Status", ret.UpdateTime);
            ret.CompositeDisposable.Add(ret.listener);

            return ret;
        }
    }
}
