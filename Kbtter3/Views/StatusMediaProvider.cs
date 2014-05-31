using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kbtter3.Views
{
    public static class StatusMediaProvider
    {
        public static IList<Func<Uri, Uri>> Providers { get; set; }

        static StatusMediaProvider()
        {
            Providers = new List<Func<Uri, Uri>>();
        }

        public static void AddProvider(Func<Uri, Uri> prvfunc)
        {
            Providers.Add(prvfunc);
        }

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
