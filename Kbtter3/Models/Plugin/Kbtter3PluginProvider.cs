﻿using System;
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
    /// <summary>
    /// Kbtter3のプラグインローダーの規約を定義します。
    /// </summary>
    public abstract class Kbtter3PluginProvider
    {
        /// <summary>
        /// 新しいインスタンスを生成します。
        /// </summary>
        public Kbtter3PluginProvider()
        {

        }

        /// <summary>
        /// 初期化します。
        /// </summary>
        /// <param name="kbtter">Kbtter3 Modelのインスタンス</param>
        public abstract void Initialize(Kbtter kbtter);

        /// <summary>
        /// プラグインを読み込みます。
        /// </summary>
        /// <param name="filenames">プラグインのファイル名リスト</param>
        public abstract void Load(IList<string> filenames);

        /// <summary>
        /// ローダーを開放します。
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mon"></param>
        public abstract void StatusUpdate(StatusMessage msg,object mon);
        public abstract void EventUpdate(EventMessage msg, object mon);
        public abstract void IdEventUpdate(IdMessage msg, object mon);
        public abstract void DirectMessageUpdate(DirectMessageMessage msg, object mon);
        public abstract void SystemRequest(string msg, object mon);
        public abstract string ProvidingLanguage { get; }
        public abstract int ProvidingPluginsCount { get; }
    }

    internal sealed class Kbtter3NativePluginProvider : Kbtter3PluginProvider
    {
        Kbtter kbtter;
        int count = 0;


        public override void Initialize(Kbtter kbtter)
        {
            kbtter = Kbtter.Instance;
        }

        public override void Load(IList<string> filenames)
        {
            
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
            get { return ""; }
        }

        public override int ProvidingPluginsCount
        {
            get { return count; }
        }
    }
}