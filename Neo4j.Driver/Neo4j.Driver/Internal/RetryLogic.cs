using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal interface IRetryLogic
    {
        T Retry<T>(Func<RetryResult<T>> workFunc);
        Task<T> RetryAsync<T>(Func<Task<RetryResult<T>>> asyncWorkFunc);
    }

    internal class RetryResult<T>
    {
        public bool Success { get; }
        public T Value { get; }
        public Exception Error { get; }

        public static RetryResult<T> FromResult(T value)
        {
            return new RetryResult<T>(value, true, null);
        }
        public static RetryResult<T> FromError(Exception e=null)
        {
            return new RetryResult<T>(default(T), false, e);
        }

        private RetryResult(T value, bool success, Exception error)
        {
            Value = value;
            Success = success;
            Error = error;
        }

        public RetryResult(bool success)
        {
            Success = success;
        }
    }

    internal class ExponentialBackoffRetryLogic : IRetryLogic
    {
        private readonly double _maxRetryTimeMs;
        private readonly double _initialRetryDelayMs;
        private readonly double _multiplier;
        private readonly double _jitterFactor;

        public static readonly int InitialTxRetryDelayMs = 1000;
        public static readonly int InitialConnAcquisitionDelayMs = 100;
        private const double RetryDelayMultiplier = 2.0;
        private const double RetryDelayJitterFactor = 0.2;


        public ExponentialBackoffRetryLogic(TimeSpan maxRetryTimeout, int initialRetryDelayMs, bool random=false)
        {
            _maxRetryTimeMs = maxRetryTimeout.TotalMilliseconds;
            _initialRetryDelayMs = random ? 
                new Random(Guid.NewGuid().GetHashCode()).Next(initialRetryDelayMs) : 
                initialRetryDelayMs;
            _multiplier = RetryDelayMultiplier;
            _jitterFactor = RetryDelayJitterFactor;
        }

        public T Retry<T>(Func<RetryResult<T>> workFunc)
        {
            AggregateException exception = null;
            var timer = new Stopwatch();
            timer.Start();
            var delayMs = _initialRetryDelayMs;
            var retryCount = 0;
            do
            {
                retryCount++;
                var result = workFunc();
                if (result.Success)
                {
                    return result.Value;
                }
                else if (result.Error != null)
                {
                    exception = exception == null ?
                        new AggregateException(result.Error) :
                        new AggregateException(exception, result.Error);
                }

                var delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                Task.Delay(delay).Wait(); // blocking for this delay
                delayMs = delayMs * _multiplier;
            } while (timer.Elapsed.TotalMilliseconds < _maxRetryTimeMs);

            timer.Stop();
            if (exception == null)
            {
                throw new ClientException($"Failed after retrying for {retryCount} times in {_maxRetryTimeMs} ms.");
            }
            else
            {
                throw exception;
            }
        }

        public async Task<T> RetryAsync<T>(Func<Task<RetryResult<T>>> asyncWorkFunc)
        {
            AggregateException exception = null;
            var timer = new Stopwatch();
            timer.Start();
            var delayMs = _initialRetryDelayMs;
            var retryCount = 0;
            do
            {
                retryCount++;
                var result = await asyncWorkFunc().ConfigureAwait(false);
                if (result.Success)
                {
                    return result.Value;
                }
                else if (result.Error != null)
                {
                    exception = exception == null ? 
                        new AggregateException(result.Error) : 
                        new AggregateException(exception, result.Error);
                }

                var delay = TimeSpan.FromMilliseconds(ComputeDelayWithJitter(delayMs));
                await Task.Delay(delay).ConfigureAwait(false); // blocking for this delay
                delayMs = delayMs * _multiplier;
            } while (timer.Elapsed.TotalMilliseconds < _maxRetryTimeMs);

            timer.Stop();
            if (exception == null)
            {
                throw new ClientException($"Failed after retrying for {retryCount} times in {_maxRetryTimeMs} ms.");
            }
            else
            {
                throw exception;
            }
        }

        private double ComputeDelayWithJitter(double delayMs)
        {
            var jitter = delayMs * _jitterFactor;
            return delayMs - jitter + 2 * jitter * new Random(Guid.NewGuid().GetHashCode()).NextDouble();
        }
    }
}