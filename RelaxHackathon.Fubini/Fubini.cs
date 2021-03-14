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
        private Memory<BigInteger> oldBinBuffer;
        /// <summary>
        /// Buffer for (n k)
        /// </summary>
        private Memory<BigInteger> binBuffer;

        private readonly Memory<BigInteger> previousResults;

        private int n;

        public Fubini(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            this.n = 0;
            binBuffer = new BigInteger[n + 1];
            oldBinBuffer = new BigInteger[n + 1];
            previousResults = new BigInteger[n + 1];
            previousResults.Span[0] = BigInteger.One;
        }

        public async Task<BigInteger> CalcAsync()
        {
            n++;
            CalcBinomialWithPascalTriangle(n);
            var result = await CalcSumAsync(n).ConfigureAwait(false);
            previousResults.Span[n] = result;
            return result;
        }

        private static Task ExecuteParallel(int n, Action<int, int> func)
            => ExecuteParallel(n, (n1, n2) => Task.Run(() => func(n1, n2)));

        private static async Task ExecuteParallel(int n, Func<int, int, Task> func)
            => await ExecuteParallel<int, int>(n, async (n1, n2) =>
            {
                await func(n1, n2).ConfigureAwait(false);
                return 0;
            }, null);

        private static Task<T2?> ExecuteParallel<T1, T2>(int n, Func<int, int, T1> func,
            Func<IEnumerable<T1>, T2>? merge)
            => ExecuteParallel(n, async (n1, n2) => await Task.Run(() => func(n1, n2)).ConfigureAwait(false), merge);

        private static async Task<T2?> ExecuteParallel<T1, T2>(int n, Func<int, int, ValueTask<T1>> func,
            Func<IEnumerable<T1>, T2>? merge)
        {
            Memory<Task<(T1?, bool)>> jobs = new Task<(T1?, bool)>[Environment.ProcessorCount];
            var slice = (int)Math.Ceiling((float)n / jobs.Length);
            int i = 0;
            for (; i < jobs.Length && i * slice < n; ++i)
            {
                var start = slice * i;
                var end = start + Math.Min(n - start, slice);
                jobs.Span[i] = Task.Run(async () => ((T1?)await func(start, end).ConfigureAwait(false), true));
            }
            for (; i < jobs.Length; ++i)
                jobs.Span[i] = Task.FromResult((default(T1), false));
            await Task.WhenAll(jobs.ToArray()).ConfigureAwait(false);
            if (merge != null)
            {
                return merge(
                    jobs.ToArray()
                        .Select(x => x.Result)
                        .Where(x => x.Item2 == true)
                        .Select(x => x.Item1)
                        .Cast<T1>()
                );
            }
            else return default;
        }

        private void CalcBinomialWithPascalTriangle(int n)
        {
            // add sums to left side
            (binBuffer, oldBinBuffer) = (oldBinBuffer, binBuffer);
            binBuffer.Span[0] = BigInteger.One;
            for (int i = 1; i < n; ++i)
            {
                binBuffer.Span[i] = oldBinBuffer.Span[i - 1] + oldBinBuffer.Span[i];
            }
            binBuffer.Span[n] = BigInteger.One;
        }

        private async Task<BigInteger> CalcSumAsync(int n)
        {
            return await ExecuteParallel(n, (i1, i2) => CalcPartialSum(n, i1, i2),
                (x) =>
                {
                    var sum = BigInteger.Zero;
                    foreach (var item in x)
                        sum += item;
                    return sum;
                });
        }

        private BigInteger CalcPartialSum(int n, int i1, int i2)
        {
            var sum = BigInteger.Zero;
            for (int i = i1; i<i2; ++i)
            {
                sum += binBuffer.Span[i + 1] * previousResults.Span[n - (i + 1)];
            }
            return sum;
        }
    }
}
