using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kbtter3.Query;

namespace Kbtter3.Query.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testの値は" + "7");
            Console.WriteLine("クエリを入力してください(exitで終了)");
            do
            {
                var qs = Console.ReadLine();
                if (qs == "exit") break;

                var q = new Kbtter3Query(qs);
                q.SetVariable("Test", 7);
                var ret = q.Execute();
                Console.WriteLine(ret.AsBoolean());

            } while (true);

            Console.WriteLine("Bye!");
            Console.ReadLine();
        }
    }
}
