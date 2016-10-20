using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools = new ConcurrentDictionary<Uri, IConnectionPool>();
        private readonly IAuthToken _authToken;
        private readonly EncryptionManager _encryptionManager;
        private readonly ConnectionPoolSettings _poolSettings;

        // for test only
        private readonly IConnectionPool _fakeConnectionPool;

        private volatile bool _disposeCalled;

        public ClusterConnectionPool(IAuthToken authToken, EncryptionManager encryptionManager, ConnectionPoolSettings poolSettings, ILogger logger)
            : base(logger)
        {
            _authToken = authToken;
            _encryptionManager = encryptionManager;
            _poolSettings = poolSettings;
        }

        internal ClusterConnectionPool(IConnectionPool connectionPool,
            ConcurrentDictionary<Uri, IConnectionPool> pool=null,
            ConnectionPoolSettings poolSettings=null,
            ILogger logger=null) :
            this(null, encryptionManager: null, poolSettings: poolSettings, logger: logger)
        {
            _fakeConnectionPool = connectionPool;
            _pools = pool;
        }

        private IConnectionPool CreateNewConnectionPool(Uri uri)
        {
            return _fakeConnectionPool ?? new ConnectionPool(uri, _authToken, _encryptionManager, _poolSettings, Logger);
        }

        public bool TryAcquire(Uri uri, out IPooledConnection conn)
        {
            IConnectionPool pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                conn = null;
                return false;
            }

            conn = pool.Acquire();
            return true;
        }

        public bool HasAddress(Uri uri)
        {
            return _pools.ContainsKey(uri);
        }

        // This is the only place to add a pool
        public void Add(Uri uri)
        {
            _pools.GetOrAdd(uri, CreateNewConnectionPool);
            if (_disposeCalled)
            {
                // Anything added after dispose should be directly cleaned.
                Purge(uri);
                throw new InvalidOperationException($"Failed to create connections with server {uri} as the driver has already started to dispose.");
            }
        }

        public void Purge(Uri uri)
        {
            IConnectionPool toRemvoe;
            var removed = _pools.TryRemove(uri, out toRemvoe);
            if (removed)
            {
                toRemvoe.Dispose();
            }
        }

        public void Clear()
        {
            var uris = _pools.Keys;
            foreach (var uri in uris)
            {
                Purge(uri);
            }
        }

        public void Release(Uri uri, Guid id)
        {
            IConnectionPool pool;
            var found = _pools.TryGetValue(uri, out pool);
            if (found)
            {
                pool.Release(id);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            _disposeCalled = true;
            Clear();
        }
    }

    internal interface IClusterConnectionPool
    {
        // Try to acquire a connection with the server specified by the uri
        bool TryAcquire(Uri uri, out IPooledConnection conn);
        // Release the connection back to the server connection pool specified by the uri
        void Release(Uri uri, Guid id);
        // Add a pool for a new uri
        void Add(Uri uri);
        // Remove all the connections with the server specified by the uri
        void Purge(Uri uri);
        // Purge all
        void Clear();
        // Test if we have established connections with the server specified by the uri
        bool HasAddress(Uri uri);
    }
}
