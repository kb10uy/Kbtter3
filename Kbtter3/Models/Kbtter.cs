using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public readonly string ConsumerTokenFileName = "config/consumer.json";
        public readonly string AccessTokenFileName = "config/users.json";
        public readonly string ConsumerDefaultKey = "5bI3XiTNEMHiamjMV5Acnqkex";
        public readonly string ConsumerDefaultSecret = "ni2jGjwKTLcdpp1x6nr3yFo9bRrSWRdZfYbzEAZLhKz4uDDErN";

        public Tokens Token { get; set; }

        public IDisposable Stream { get; protected set; }

        public List<Status> Cache { get; set; }

        public List<AccessToken> AccessTokens { get; set; }

        public ConsumerToken ConsumerToken { get; private set; }

        public event Action<StatusMessage> OnStatus;
        public event Action<EventMessage> OnEvent;

        public StatusMessage LatestStatus { get; set; }
        public EventMessage LatestEvent { get; set; }

        public OAuth.OAuthSession OAuthSession { get; set; }

        public Queue<Status> ShowingStatuses { get; private set; }

        private Kbtter()
        {

        }

        ~Kbtter()
        {
            StopStreaming();
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
            ShowingStatuses = new Queue<Status>();

            AccessTokens = new List<AccessToken>();
            if (!Directory.Exists("config")) Directory.CreateDirectory("config");
            if (!File.Exists(ConsumerTokenFileName))
            {
                var ct = new ConsumerToken { Key = ConsumerDefaultKey, Secret = ConsumerDefaultSecret };
                var json = JsonConvert.SerializeObject(ct);
                File.WriteAllText(ConsumerTokenFileName, json);
            }
            if (!File.Exists(AccessTokenFileName)) SaveAccessTokens();

            var cjs = File.ReadAllText(ConsumerTokenFileName);
            ConsumerToken = JsonConvert.DeserializeObject<ConsumerToken>(cjs);
            OAuthSession = OAuth.Authorize(ConsumerToken.Key, ConsumerToken.Secret);

            var ajs = File.ReadAllText(AccessTokenFileName);
            AccessTokens = JsonConvert.DeserializeObject<List<AccessToken>>(ajs);

            RaisePropertyChanged("AccessTokenRequest");
        }

        #region 認証
        public void AuthenticateWith(int ai)
        {
            Token = Tokens.Create(
                ConsumerToken.Key,
                ConsumerToken.Secret,
                AccessTokens[ai].Token,
                AccessTokens[ai].TokenSecret
                );
            Cache = new List<Status>();
        }

        public Tokens AuthorizeToken(string pin)
        {
            var t = OAuthSession.GetTokens(pin);
            return t;
        }
        #endregion

        #region streaming
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
            ShowingStatuses.Enqueue(msg.Status);
            RaisePropertyChanged("Status");
        }

        void NotifyEventUpdate(EventMessage msg)
        {
            LatestEvent = msg;
            RaisePropertyChanged("Event");
        }

        public void StopStreaming()
        {
            Stream.Dispose();
        }

        #endregion

        #region コンフィグ用メソッド

        public void AddToken(Tokens t)
        {
            AccessTokens.Add(new AccessToken { ScreenName = t.ScreenName, Token = t.AccessToken, TokenSecret = t.AccessTokenSecret });
            SaveAccessTokens();
        }

        public void SaveAccessTokens()
        {
            var json = JsonConvert.SerializeObject(AccessTokens);
            File.WriteAllText(AccessTokenFileName, json);
        }

        #endregion


    }

    public class AccessToken
    {
        public string ScreenName { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
    }

    public class ConsumerToken
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
