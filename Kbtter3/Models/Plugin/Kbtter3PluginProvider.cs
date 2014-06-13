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
        /// <param name="filenames">
        /// プラグインのファイル名リスト。
        /// 全て小文字で返されます。
        /// </param>
        /// <returns>エラーのあったプラグインの数</returns>
        public abstract int Load(IList<string> filenames);

        /// <summary>
        /// ローダーを開放します。
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 受信したツイートについて、スクリプトの処理をします。
        /// </summary>
        /// <param name="msg">StatusMessage</param>
        /// <param name="mon">非同期的に処理するためのモニター用オブジェクト</param>
        public abstract void StatusUpdate(StatusMessage msg, object mon);

        /// <summary>
        /// 受信したイベントについて、スクリプトの処理をします。
        /// </summary>
        /// <param name="msg">EventMessage</param>
        /// <param name="mon">非同期的に処理するためのモニター用オブジェクト</param>
        public abstract void EventUpdate(EventMessage msg, object mon);

        /// <summary>
        /// 受信したIDのイベントについて、スクリプトの処理をします。
        /// </summary>
        /// <param name="msg">IdMessage</param>
        /// <param name="mon">非同期的に処理するためのモニター用オブジェクト</param>
        public abstract void IdEventUpdate(IdMessage msg, object mon);

        /// <summary>
        /// 受信したダイレクトメッセージについて、スクリプトの処理をします。
        /// </summary>
        /// <param name="msg">DirectMessageMessage</param>
        /// <param name="mon">非同期的に処理するためのモニター用オブジェクト</param>
        public abstract void DirectMessageUpdate(DirectMessageMessage msg, object mon);

        /// <summary>
        /// Kbtter3側から特殊なメッセージを送信し、処理します。
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mon"></param>
        public abstract void SystemRequest(string msg, object mon);

        /// <summary>
        /// このローダーが連携している言語名を取得します。
        /// </summary>
        public abstract string ProvidingLanguage { get; }

        /// <summary>
        /// 現在このローダーが読み込んでいるプラグイン数を取得します。
        /// </summary>
        public abstract int ProvidingPluginsCount { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Kbtter3PluginEventProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public Kbtter Instance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public event Action<StatusMessage> StatusReceived;

        /// <summary>
        /// 
        /// </summary>
        public event Action<EventMessage> EventReceived;

        /// <summary>
        /// 
        /// </summary>
        public event Action<IdMessage> IdReceived;

        /// <summary>
        /// 
        /// </summary>
        public event Action<DirectMessageMessage> DirectMessageReceived;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void Tweet(string text)
        {
            try
            {
                Instance.Token.Statuses.UpdateAsync(Status => text);
            }
            catch
            { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="text"></param>
        public void Reply(long id, string text)
        {
            try
            {
                Instance.Token.Statuses.Update(Status => text, in_reply_to_status_id => id);
            }
            catch
            { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fid"></param>
        public void Favorite(long fid)
        {
            try
            {
                Instance.Token.Favorites.CreateAsync(id => fid);
            }
            catch
            { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fid"></param>
        public void Unfavorite(long fid)
        {
            try
            {
                Instance.Token.Favorites.DestroyAsync(id => fid);
            }
            catch
            { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rid"></param>
        public void Retweet(long rid)
        {
            try
            {
                Instance.Token.Statuses.RetweetAsync(id => rid);
            }
            catch
            { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="did"></param>
        public void Delete(long did)
        {
            try
            {
                Instance.Token.Statuses.DestroyAsync(id => did);
            }
            catch
            { }
        }

        #region イベント励起
        internal void RaiseStatus(StatusMessage msg)
        {
            if (StatusReceived != null) StatusReceived(msg);
        }

        internal void RaiseEvent(EventMessage msg)
        {
            if (StatusReceived != null) EventReceived(msg);
        }

        internal void RaiseIdEvent(IdMessage msg)
        {
            if (StatusReceived != null) IdReceived(msg);
        }

        internal void RaiseDirectMessage(DirectMessageMessage msg)
        {
            if (StatusReceived != null) DirectMessageReceived(msg);
        }
        #endregion

    }
}
