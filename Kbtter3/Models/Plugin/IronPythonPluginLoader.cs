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

using Microsoft.Scripting.Hosting;
using IronPython.Hosting;

namespace Kbtter3.Models.Plugin
{
    internal sealed class Kbtter3IronPythonPluginLoader : Kbtter3PluginProvider
    {

        Kbtter3PluginEventProvider ep;

        public override string ProvidingLanguage
        {
            get
            {
                return "IronPython";
            }
        }

        int count = 0;
        public override int ProvidingPluginsCount
        {
            get { return count; }
        }

        public override void StatusUpdate(StatusMessage msg, object mon)
        {
            ep.RaiseStatus(msg);
        }

        public override void DirectMessageUpdate(DirectMessageMessage msg, object mon)
        {
            ep.RaiseDirectMessage(msg);
        }

        public override void EventUpdate(EventMessage msg, object mon)
        {
            ep.RaiseEvent(msg);
        }

        public override void IdEventUpdate(IdMessage msg, object mon)
        {
            ep.RaiseIdEvent(msg);
        }

        public override void Initialize(Kbtter kbtter)
        {
            ep = new Kbtter3PluginEventProvider { Instance = kbtter };
        }

        public override int Load(IList<string> filenames)
        {
            var list = filenames.Where(p => p.EndsWith(".py"));
            var err = 0;
            var eng = Python.CreateEngine();
            var scp = eng.CreateScope();
            scp.SetVariable("Kbtter3", ep);
            foreach (var i in list)
            {
                var src = eng.CreateScriptSourceFromFile(i);
                try
                {
                    src.Execute(scp);
                }
                catch (Exception e)
                {
                    Kbtter.Instance.LogError(string.Format("{0}の読み込み中にエラーが発生しました \n{1}", i, e.Message));
                    err++;
                }

            }
            count = list.Count();
            return err;
        }

        public override void Release()
        {

        }

        public override void SystemRequest(string msg, object mon)
        {

        }
    }

}
