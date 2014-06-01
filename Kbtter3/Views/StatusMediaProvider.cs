using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kbtter3.Views
{
    /// <summary>
    /// ツイート内で表示する画像のUriの抽出アルゴリズムの管理をします。
    /// </summary>
    public static class StatusMediaProvider
    {
        /// <summary>
        /// 抽出アルゴリズム
        /// </summary>
        public static IList<Func<Uri, Uri>> Providers { get; set; }

        static StatusMediaProvider()
        {
            Providers = new List<Func<Uri, Uri>>();
        }

        /// <summary>
        /// アルゴリズムを追加します。
        /// </summary>
        /// <param name="prvfunc">抽出アルゴリズムのメソッド</param>
        public static void AddProvider(Func<Uri, Uri> prvfunc)
        {
            Providers.Add(prvfunc);
        }

        /// <summary>
        /// 現在の状態で、指定したUriを直接的な画像のUriに変換することを試みます。
        /// </summary>
        /// <param name="euri">変換するUri。</param>
        /// <returns>変換できた場合はそのUri、出来なければnull。</returns>
        public static Uri TryGetDirectMediaUri(Uri euri)
        {
            foreach (var i in Providers)
            {
                var u = i(euri);
                if (u != null) return u;
            }
            return null;
        }
    }
}
