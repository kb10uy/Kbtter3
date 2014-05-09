using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using CoreTweet;
using CoreTweet.Core;
using CoreTweet.Rest;
using CoreTweet.Streaming;
using CoreTweet.Streaming.Reactive;

using System.Reactive;
using System.Reactive.Linq;

using Livet;

namespace Kbtter3.Models
{
    public class Kbtter : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        public Tokens Token { get; set; }

        public IDisposable Stream { get; protected set; }

        public List<Status> Cache { get; set; }

        public KbtterTwitterSettings Settings { get; set; }

        public event Action<StatusMessage> OnStatus;
        public event Action<EventMessage> OnEvent;

        public StatusMessage LatestStatus { get; set; }
        public EventMessage LatestEvent { get; set; }

        public OAuth.OAuthSession OAuthSession { get; set; }

        private Kbtter()
        {

        }

        #region シングルトン
        static Kbtter _instance;
        public static Kbtter Instance
        {
            get
            {
                if (_instance == null) _instance = new Kbtter();
                return _instance;
            }
        }
        #endregion

        public void Initialize()
        {
            Settings = new KbtterTwitterSettings();
            OAuthSession = OAuth.Authorize("", "");
            RaisePropertyChanged("RequestTokens");
        }



        public void AuthenticateWith(int ci, int ai)
        {
            Token = Tokens.Create(
                Settings.ConsumerTokens[ci].Key,
                Settings.ConsumerTokens[ci].Secret,
                Settings.AccessTokens[ai].Token,
                Settings.AccessTokens[ai].TokenSecret
                );
            Cache = new List<Status>();
        }

        public void StartStreaming()
        {
            var ob = Token.Streaming.StartObservableStream(StreamingType.User)
                .Publish();
            ob.OfType<StatusMessage>().Subscribe((p) =>
            {
                if (OnStatus != null) OnStatus(p);
            });
            ob.OfType<EventMessage>().Subscribe((p) =>
            {
                if (OnEvent != null) OnEvent(p);
            });
            Stream = ob.Connect();

            OnStatus += NotifyStatusUpdate;
            OnEvent += NotifyEventUpdate;
        }

        void NotifyStatusUpdate(StatusMessage msg)
        {
            LatestStatus = msg;
            RaisePropertyChanged("Status");
        }

        void NotifyEventUpdate(EventMessage msg)
        {
            LatestEvent = msg;
            RaisePropertyChanged(() => "");
        }

        public void StopStreaming()
        {
            Stream.Dispose();
        }
    }

    public class KbtterTwitterSettings
    {
        public List<ConsumerToken> ConsumerTokens { get; set; }
        public List<AccessToken> AccessTokens { get; set; }

        public KbtterTwitterSettings()
        {
            ConsumerTokens = new List<ConsumerToken>();
            AccessTokens = new List<AccessToken>();

            ConsumerTokens.Add(new ConsumerToken
            {
                Name = "デフォルト",
                Key = "5bI3XiTNEMHiamjMV5Acnqkex",
                Secret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN"
            });
        }
    }

    public class AccessToken
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
    }

    public class ConsumerToken
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
