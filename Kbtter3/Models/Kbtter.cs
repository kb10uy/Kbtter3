using System;
using System.Collections.Generic;
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

        public event Action<StatusMessage> OnStatus;
        public event Action<EventMessage> OnEvent;

        public StatusMessage LatestStatus { get; set; }
        public EventMessage LatestEvent { get; set; }

        public void Initialize(string at, string ats)
        {
            Token = Tokens.Create("", "", at, ats);
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
            RaisePropertyChanged(() => LatestStatus);
        }

        void NotifyEventUpdate(EventMessage msg)
        {
            LatestEvent = msg;
            RaisePropertyChanged(()=>LatestEvent);
        }

        public void StopStreaming()
        {
            Stream.Dispose();
        }
    }
}
