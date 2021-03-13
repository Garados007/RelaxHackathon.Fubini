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
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode 
                = System.Runtime.GCLargeObjectHeapCompactionMode.Default;
            var fubini = new Fubini(n);
            await fubini.CalcBufferAsync().ConfigureAwait(false);
            if (nooutput)
            {
                for (int i = 0; i < n; ++i)
                {
                    await fubini.CalcAsync(i).ConfigureAwait(false);
                }
            }
            else
            {
                Console.Write("[");
                for (int i = 0; i < n; ++i)
                {
                    Console.WriteLine(i == 0 ? "" : ",");
                    Console.Write($"  {await fubini.CalcAsync(i).ConfigureAwait(false)}");
                }
                Console.WriteLine();
                Console.WriteLine("]");
            }
            return 0;
        }
    }
}
