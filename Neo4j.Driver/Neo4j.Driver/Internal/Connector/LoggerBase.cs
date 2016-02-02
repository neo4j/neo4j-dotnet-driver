using System;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    public abstract class LoggerBase : IDisposable
    {
        protected LoggerBase(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; private set; }

        protected void TryExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected T TryExecute<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected async Task TryExecuteAsync(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected async Task<T> TryExecuteAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.Message, ex);
                throw;
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Logger = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}