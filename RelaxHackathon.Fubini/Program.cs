using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Numerics;

namespace RelaxHackathon.Fubini
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("n expected");
                return 1;
            }
            var nooutput = args.Length >= 2 && args[0] == "--no-out";
            if (!int.TryParse(args[nooutput ? 1 : 0], out int n))
            {
                Console.Error.WriteLine("invalid n");
                return 2;
            }
            if (n < 0)
            {
                Console.Error.WriteLine("n is smaller than 0");
                return 3;
            }
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode 
                = System.Runtime.GCLargeObjectHeapCompactionMode.Default;
            //var hasStarted = GC.TryStartNoGCRegion(8_000_000_000);
            var fubini = new Fubini(n);
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            //GC.EndNoGCRegion();
            if (nooutput)
            {
                for (int i = 1; i < n; ++i)
                {
                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;

                    await fubini.CalcAsync().ConfigureAwait(false);

                    //if (i % 100 == 0)
                    //    GC.Collect(0, GCCollectionMode.Optimized, false, false);

                }
            }
            else
            {
                Console.WriteLine("[");
                Console.Write("  1");
                for (int i = 1; i < n; ++i)
                {
                    Console.WriteLine(",");

                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;

                    var num = await fubini.CalcAsync().ConfigureAwait(false);
                    Console.Write($"  {num}");

                    //if (i % 100 == 0)
                    //    GC.Collect(0, GCCollectionMode.Optimized, false, false);
                }
                Console.WriteLine();
                Console.WriteLine("]");
            }
            //if (hasStarted)
            //    GC.EndNoGCRegion();
            return 0;
        }
    }
}
