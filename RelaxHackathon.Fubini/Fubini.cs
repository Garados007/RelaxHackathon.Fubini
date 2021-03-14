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
            previousResults.Span[0] = binBuffer.Span[0] = oldBinBuffer.Span[0] = BigInteger.One;
            backgroundJob = CalcBinomialWithPascalTriangleAsync(1);
        }

        private Task backgroundJob;

        public async Task<BigInteger> CalcAsync()
        {
            n++;
            // wait for the background job to finish. These will be the the new coefficents
            await backgroundJob.ConfigureAwait(false);
            // flip the buffers. The background job has created the new values on the old buffer
            (binBuffer, oldBinBuffer) = (oldBinBuffer, binBuffer);
            // initialize the new background job
            backgroundJob = CalcBinomialWithPascalTriangleAsync(n + 1);

            // calculate the new Fubini value
            var result = await CalcSumAsync(n).ConfigureAwait(false);
            // store this because it will be needed in future
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

        /// <summary>
        /// This updates the old buffer with the Pascal Triangle. This will be used in the next instance
        /// </summary>
        /// <param name="n">the next n</param>
        /// <returns>the execution task</returns>
        private async Task CalcBinomialWithPascalTriangleAsync(int n)
        {
            oldBinBuffer.Span[0] = BigInteger.One;
            await ExecuteParallel(n - 1, (i1, i2) =>
            {
                for (int i = i1 + 1; i <= i2; ++i)
                {
                    oldBinBuffer.Span[i] = binBuffer.Span[i - 1] + binBuffer.Span[i];
                }
            }).ConfigureAwait(false);
            oldBinBuffer.Span[n] = BigInteger.One;
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
