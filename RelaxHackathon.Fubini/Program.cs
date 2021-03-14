using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
            var hasStarted = GC.TryStartNoGCRegion(8_000_000_000);
            var fubini = new Fubini(n);
            await fubini.CalcBufferAsync().ConfigureAwait(false);
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            if (nooutput)
            {
                for (int i = 0; i < n; ++i)
                {
                    //System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;

                    await fubini.CalcAsync(i).ConfigureAwait(false);

                    GC.Collect(0, GCCollectionMode.Optimized, false, false);

                    //if (i % 10 == 0)
                    //{
                    //    Console.WriteLine($"[{watch.Elapsed}] {i}");
                    //}

                    //if (watch.ElapsedMilliseconds > 100_000)
                    //{
                    //    Console.WriteLine(i);
                    //    return 5;
                    //}
                }
            }
            else
            {
                Console.Write("[");
                for (int i = 0; i < n; ++i)
                {
                    Console.WriteLine(i == 0 ? "" : ",");

                    //System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;

                    var num = await fubini.CalcAsync(i).ConfigureAwait(false);
                    Console.Write($"  {num}");

                    GC.Collect(0, GCCollectionMode.Optimized, false, false);
                }
                Console.WriteLine();
                Console.WriteLine("]");
            }
            if (hasStarted)
                GC.EndNoGCRegion();
            return 0;
        }
    }
}
