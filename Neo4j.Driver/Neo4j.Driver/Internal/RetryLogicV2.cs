using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal interface IRetryLogicV2
    {
        Task<T> RetryAsync<T>(Func<Task<Tuple<T,bool>>> asyncWorkFunc);
    }

    internal class ExponentialBackoffRetryLogicV2 : IRetryLogicV2
    {
        private readonly double _maxRetryTimeMs;
        private readonly double _initialRetryDelayMs;
        private readonly double _multiplier;
        private readonly double _jitterFactor;

        private static readonly double InitialRetryDelayMs = TimeSpan.FromSeconds(1).TotalMilliseconds;
        private const double RetryDelayMultiplier = 2.0;
        private const double RetryDelayJitterFactor = 0.2;


        public ExponentialBackoffRetryLogicV2(TimeSpan maxRetryTimeout, TimeSpan initialRetryDelay)
        {
            _maxRetryTimeMs = maxRetryTimeout.TotalMilliseconds;
            _initialRetryDelayMs = new Random(Guid.NewGuid().GetHashCode()).Next((int)initialRetryDelay.TotalMilliseconds);
            _multiplier = RetryDelayMultiplier;
            _jitterFactor = RetryDelayJitterFactor;
        }

        public async Task<T> RetryAsync<T>(Func<Task<Tuple<T,bool>>> asyncWorkFunc)
        {
            var timer = new Stopwatch();
            timer.Start();
            var delayMs = _initialRetryDelayMs;
            do
            {
                var result = await asyncWorkFunc().ConfigureAwait(false);
                if (result.Item2)
                {
                    return result.Item1;
                }

                var delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                await Task.Delay(delay).ConfigureAwait(false); // blocking for this delay
                delayMs = delayMs * _multiplier;
            } while (timer.Elapsed.TotalMilliseconds < _maxRetryTimeMs);

            timer.Stop();
            throw new ClientException($"Failed after retry for {_maxRetryTimeMs} ms");
        }

        private double ComputeDelayWithJitter(double delayMs)
        {
            var jitter = delayMs * _jitterFactor;
            return delayMs - jitter + 2 * jitter * new Random(Guid.NewGuid().GetHashCode()).NextDouble();
        }
    }
}