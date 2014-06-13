using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using CoreTweet;
using CoreTweet.Core;
using CoreTweet.Rest;
using CoreTweet.Streaming;
using CoreTweet.Streaming.Reactive;


namespace Kbtter3.Models.Plugin
{
    internal sealed class Kbtter3NativePluginProvider : Kbtter3PluginProvider
    {
        //Kbtter kbtter;
        int count = 0;


        public override void Initialize(Kbtter kbtter)
        {
            //kbtter = Kbtter.Instance;
        }

        public override int Load(IList<string> filenames)
        {
            return 0;
        }

        public override void Release()
        {

        }

        public override void StatusUpdate(StatusMessage msg, object mon)
        {

        }

        public override void EventUpdate(EventMessage msg, object mon)
        {

        }

        public override void IdEventUpdate(IdMessage msg, object mon)
        {

        }

        public override void DirectMessageUpdate(DirectMessageMessage msg, object mon)
        {

        }

        public override void SystemRequest(string msg, object mon)
        {

        }

        public override string ProvidingLanguage
        {
            get { return ".NET(CLR)"; }
        }

        public override int ProvidingPluginsCount
        {
            get { return count; }
        }
    }

}
