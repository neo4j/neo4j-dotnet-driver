using System;

namespace Neo4j.Driver
{
    public class Driver : IDisposable
    {
        private readonly Config _config;
        private readonly Uri _url;

        internal Driver(Uri url, Config config)
        {
            _url = url;
            _config = config;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Establish a session with Neo4j instance
        /// </summary>
        /// <returns>
        ///     An <see cref="ISession" /> that could be used to <see cref="ISession.Run" /> a statement or begin a
        ///     transaction
        /// </returns>
        public ISession Session()
        {
            return new InternalSession(_url, _config);
        }
    }
}