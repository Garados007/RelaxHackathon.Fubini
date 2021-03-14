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
        ///// <summary>
        ///// Buffer for i!
        ///// </summary>
        //private readonly Memory<BigInteger> facBuffer;
        /// <summary>
        /// Buffer for i^n
        /// </summary>
        private readonly Memory<BigInteger> powerBuffer;
        private int lastPower = 0;
        /// <summary>
        /// Buffer for (n k)
        /// </summary>
        private readonly Memory<Memory<BigInteger>> binBuffer;

        private readonly int n;

        public Fubini(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            this.n = n;
            //facBuffer = new BigInteger[n + 1];
            powerBuffer = new BigInteger[n + 1];
            binBuffer = new Memory<BigInteger>[n + 1];
            for (int i = 0; i <= n; ++i)
            {
                binBuffer.Span[i] = new BigInteger[n + 1];
            }
        }

        public async Task CalcBufferAsync()
        {
            await ExecuteParallel(n + 1, CalcAllBinomial).ConfigureAwait(false);
            //await CalcFacsAsync().ConfigureAwait(false);
        }

        public async Task<BigInteger> CalcAsync(int n)
        {
            var powerFunc = CalcPowers(n);
            if (powerFunc != null)
                await ExecuteParallel(powerBuffer.Length, powerFunc).ConfigureAwait(false);
            return await CalcFullSum(n).ConfigureAwait(false);
        }

        //private Task CalcFacsAsync()
        //{
        //    return Task.Run(() =>
        //    {
        //        var fac = BigInteger.One;
        //        for (int i = 0; i <= n; ++i, fac *= i)
        //            facBuffer.Span[i] = fac;
        //    });
        //}

        private static Task ExecuteParallel(int n, Action<int, int> func)
            => ExecuteParallel(n, (n1, n2) => Task.Run(() => func(n1, n2)));

        private static async Task ExecuteParallel(int n, Func<int, int, Task> func)
        {
            Memory<Task> jobs = new Task[Environment.ProcessorCount];
            var slice = (int)Math.Ceiling((float)n / jobs.Length);
            int i = 0;
            for (; i < jobs.Length && i * slice < n; ++i)
            {
                var start = slice * i;
                var end = start + Math.Min(n - start, slice);
                jobs.Span[i] = func(start, end);
            }
            for (; i < jobs.Length; ++i)
                jobs.Span[i] = Task.CompletedTask;
            await Task.WhenAll(jobs.ToArray()).ConfigureAwait(false);
        }

        private Action<int, int>? CalcPowers(int n)
        {
            var lastPower = this.lastPower;
            this.lastPower = n;
            // if n = 0 all values are set to 1
            if (n == 0)
                return (n1, n2) =>
                {
                    for (int i = n1; i < n2; ++i)
                    {
                        powerBuffer.Span[i] = BigInteger.One;
                    }
                };
            // if n hasn't change -> complete
            if (n == lastPower)
                return null;
            // if the difference is not a one
            if (n - lastPower != 1)
                return (n1, n2) =>
                {
                    for (int i = n1; i < n2; ++i)
                    {
                        powerBuffer.Span[i] = BigInteger.Pow(i, n);
                    }
                };
            // multiply each value
            return (n1, n2) =>
            {
                for (int i = n1; i < n2; ++i)
                {
                    powerBuffer.Span[i] *= i;
                }
            };
        }

        private void CalcAllBinomial(int n1, int n2)
        {
            for (int n = n1; n< n2; ++n)
                CalcBinomial(n);
        }

        private void CalcBinomial(int n)
        {
            for (var k = 0; k <= n; ++k)
            {
                var result = CalcBinomial(n, k);
                binBuffer.Span[n].Span[k] = ((n - k) & 0x01) == 0x01 // (-1) ^ (n - k)
                    ? BigInteger.Negate(result) 
                    : result;
            }
        }

        // https://stackoverflow.com/a/9620533/12469007
        private static BigInteger CalcBinomial(int n, int k)
        {
            if (k > n - k)
                k = n - k;
            BigInteger result = BigInteger.One;
            for (int i = 1; i <= k; ++i)
            {
                result *= n - k + i;
                result /= i;
            }
            return result;
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
            // calcs (-1)^(k - j) *  k! * (j! * (k-j)!)
            var factor2 = binBuffer.Span[k].Span[j];
            // calcs j^n
            var factor3 = powerBuffer.Span[j];
            return factor2 * factor3;
        }
    }
}
