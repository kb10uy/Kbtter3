using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;

using CoreTweet;
using CoreTweet.Core;
using CoreTweet.Rest;
using CoreTweet.Streaming;
using CoreTweet.Streaming.Reactive;

using System.Reactive;
using System.Reactive.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


using Livet;

namespace Kbtter3.Models
{
    public class Kbtter : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        public readonly string ConsumerTokenFileName = "consumer.json";
        public readonly string ConsumerDefaultKey = "5bI3XiTNEMHiamjMV5Acnqkex";
        public readonly string ConsumerDefaultSecret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN";

        public Tokens Token { get; set; }

        public IDisposable Stream { get; protected set; }

        public List<Status> Cache { get; set; }

        public KbtterAccessTokens Settings { get; set; }

        public ConsumerToken ConsumerToken { get; private set; }

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
            Settings = new KbtterAccessTokens();
            if (!File.Exists(ConsumerTokenFileName))
            {
                var ct = new ConsumerToken { Key = ConsumerDefaultKey, Secret = ConsumerDefaultSecret };
                var json = JsonConvert.SerializeObject(ct);
                File.WriteAllText(ConsumerTokenFileName, json);
            }
            var cjs = File.ReadAllText(ConsumerTokenFileName);
            ConsumerToken = JsonConvert.DeserializeObject<ConsumerToken>(cjs);
            OAuthSession = OAuth.Authorize(ConsumerToken.Key, ConsumerToken.Secret);
            RaisePropertyChanged("AccessTokenRequest");
        }

        public Uri GetAuthorizationUri()
        {
            return OAuthSession.AuthorizeUri;
        }

        public void AuthenticateWith(int ci, int ai)
        {
            Token = Tokens.Create(
                ConsumerToken.Key,
                ConsumerToken.Secret,
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

    public class KbtterAccessTokens
    {
        public List<AccessToken> AccessTokens { get; set; }

        public KbtterAccessTokens()
        {
            AccessTokens = new List<AccessToken>();
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
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
