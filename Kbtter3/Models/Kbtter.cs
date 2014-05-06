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

        public void Initialize(string at, string ats)
        {
            Token = Tokens.Create("", "", at, ats);
        }

        public void StartStreaming()
        {
            var ob = Token.Streaming.StartObservableStream(StreamingType.User).Publish();

            Stream = ob.Connect();
        }

        public void StopStreaming()
        {
            Stream.Dispose();
        }
    }
}
