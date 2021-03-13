using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RelaxHackathon.Fubini
{
    public class Fubini
    {
        /// <summary>
        /// Buffer for i!
        /// </summary>
        private readonly Memory<BigInteger> facBuffer;
        /// <summary>
        /// Buffer for i^n
        /// </summary>
        private readonly Memory<BigInteger> powerBuffer;

        private readonly int n;

        public Fubini(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            this.n = n;
            facBuffer = new BigInteger[n + 1];
            powerBuffer = new BigInteger[n + 1];
            //binomialBuffer = new Memory<BigInteger>[n + 1];
            //for (int i = 0; i <= n; ++i)
            //{
            //    binomialBuffer.Span[i] = new BigInteger[n + 1];
            //}
        }

        public async Task CalcBufferAsync()
        {
            await CalcFacsAsync().ConfigureAwait(false);
            //await CalcBinomialAsync().ConfigureAwait(false);
        }

        public async Task<BigInteger> CalcAsync(int n)
        {
            await CalcPowersAsync(n).ConfigureAwait(false);
            return await CalcFullSum(n).ConfigureAwait(false);
        }

        private Task CalcFacsAsync()
        {
            return Task.Run(() =>
            {
                var fac = BigInteger.One;
                for (int i = 0; i <= n; ++i, fac *= i)
                    facBuffer.Span[i] = fac;
            });
        }

        private Task CalcPowersAsync(int n)
        {
            return Task.Run(() =>
            {
                for (int i = 0; i <= n; ++i)
                {
                    powerBuffer.Span[i] = BigInteger.Pow(i, n);
                }
            });
        }

        private static async Task<BigInteger> CalcSumAsync(int max, Func<int, Task<BigInteger>> func)
        {
            Memory<Task<BigInteger>> jobs = new Task<BigInteger>[max];
            for (int i = 0; i < max; ++i)
                jobs.Span[i] = func(i);
            await Task.WhenAll(jobs.ToArray()).ConfigureAwait(false);
            var sum = BigInteger.Zero;
            for (int i = 0; i< max ; ++i)
                sum += jobs.Span[i].Result;
            return sum;
        }

        //private async Task CalcBinomialAsync()
        //{
        //    Memory<Task> jobs = new Task[Environment.ProcessorCount];
        //    var n = this.n + 1;
        //    var slice = n / jobs.Length;
        //    for (int i = 0; i< jobs.Length; ++i)
        //    {
        //        var low = slice * i;
        //        var high = low + Math.Min(n - low, slice);
        //        jobs.Span[i] = CalcBinomialAsync(low, high);
        //    }
        //    await Task.WhenAll(jobs.ToArray()).ConfigureAwait(false);
        //}


        //private Task CalcBinomialAsync(int k1, int k2)
        //{
        //    return Task.Run(() =>
        //    {
        //        for (int k = k1; k < k2; ++k)
        //        {
        //            CalcBinomial(k);
        //            GC.Collect(0, GCCollectionMode.Optimized, false, false);
        //        }
        //    });
        //}

        //private void CalcBinomial(int k)
        //{
        //    for (int j = 0; j <= k; ++j)
        //        binomialBuffer.Span[k].Span[j] = CalcBinomial(k, j);
        //}

        //private BigInteger CalcBinomial(int k, int j)
        //{
        //    var span = facBuffer.Span;
        //    return span[k] / (span[j] * span[k - j]);
        //}

        private async Task<BigInteger> CalcFullSum(int n)
        {
            return await CalcSumAsync(n + 1, async k => await CalcEntryAsync(k, n).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private Task<BigInteger> CalcEntryAsync(int k, int n)
        {
            return Task.Run(() => CalcEntry(k, n));
        }

        private BigInteger CalcEntry(int k, int n)
        {
            var sum = BigInteger.Zero;
            for (int j = 0; j <= k; ++j)
                sum += CalcEntry(k, j, n);
            return sum;
        }

        private BigInteger CalcEntry(int k, int j, int _)
        {
            // calcs (-1)^(k - j)
            var factor1 = ((k - j) & 0x01) == 0x01 ? -1 : 1;
            // calcs k! * (j! * (k-j)!)
            var bin = facBuffer.Span;
            var factor2 = bin[k] / (bin[j] * bin[k - j]);
            // calcs j^n
            var factor3 = powerBuffer.Span[j];
            return factor1 * factor2 * factor3;
        }
    }
}
