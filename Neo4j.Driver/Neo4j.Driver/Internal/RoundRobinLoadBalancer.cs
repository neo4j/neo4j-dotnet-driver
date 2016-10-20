using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal interface ILoadBalancer
    {
        IPooledConnection AcquireConnection(AccessMode mode);
    }

    internal class RoundRobinLoadBalancer : ILoadBalancer
    {
        private RoundRobinClusterView _clusterView;
        private IClusterConnectionPool _connectionPool;

        public RoundRobinLoadBalancer(
            Uri seedServer,
            IAuthToken authToken,
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            _connectionPool = new ClusterConnectionPool(authToken, encryptionManager, poolSettings, logger);
            _connectionPool.Add(seedServer);
            _clusterView = new RoundRobinClusterView(seedServer);
        }

        public IPooledConnection AcquireConnection(AccessMode mode)
        {
            Discovery();
            switch (mode)
            {
                case AccessMode.Read:
                    return AcquireReadConnection();
                case AccessMode.Write:
                    return AcquireWriteConnection();
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }
        }

        private IPooledConnection AcquireReadConnection()
        {
            while (true)
            {
                Uri uri;
                if (!_clusterView.TryNextReader(out uri))
                {
                    // no server known to clusterView
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
                    {
                        return conn;
                    }
                }
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }
            throw new SessionExpiredException("Failed to connect to any read server.");
        }

        private IPooledConnection AcquireWriteConnection()
        {
            while(true)
            {
                Uri uri;
                if (!_clusterView.TryNextWriter(out uri))
                {
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
                    {
                        return conn;
                    }
                }
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }
            throw new SessionExpiredException("Failed to connect to any write server.");
        }

        public void Forget(Uri uri)
        {
            _clusterView.Remove(uri);
            _connectionPool.Purge(uri);
        }

        // TODO: Should sync on this method
        public void Discovery()
        {
            if (!_clusterView.IsStale())
            {
                return;
            }

            var oldServers = _clusterView.All();
            var newView = NewClusterView();
            var newServers = newView.All();

            oldServers.ExceptWith(newServers);
            foreach (var server in oldServers)
            {
                _connectionPool.Purge(server);
            }
            foreach (var server in newServers)
            {
                _connectionPool.Add(server);
            }
            
            _clusterView = newView;
        }

        public RoundRobinClusterView NewClusterView()
        {
            while (true)
            {
                Uri uri;
                if (!_clusterView.TryNextRouter(out uri))
                {
                    // no alive server
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
                    {
                        var discoveryManager = new ClusterDiscoveryManager(conn);
                        discoveryManager.Rediscovery();
                        return new RoundRobinClusterView(discoveryManager.Routers, discoveryManager.Readers, discoveryManager.Writers);
                    }
                }
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }

            // TODO also try each detached routers
            throw new SessionExpiredException(
                "Failed to connect to any routing server. " +
                "Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }
    }

    internal class ClusterDiscoveryManager
    {
        private readonly IPooledConnection _conn;
        private ILogger logger;
        public IEnumerable<Uri> Readers { get; internal set; } // = new Uri[0];
        public IEnumerable<Uri> Writers { get; internal set; } // = new Uri[0];
        public IEnumerable<Uri> Routers { get; internal set; } // = new Uri[0];

        private const string ProcedureName = "dbms.cluster.routing.getServers";
        public ClusterDiscoveryManager(IPooledConnection connection)
        {
            _conn = connection;
        }

        public void Rediscovery()
        {
            using (var session = new Session(_conn, logger))
            {
                var result = session.Run($"CALL {ProcedureName}");
                var record = result.Single();
                foreach (var servers in record["servers"].As<IList<IDictionary<string,object>>>())
                {
                    var addresses = servers["addresses"].As<IList<string>>();
                    var role = servers["role"].As<string>();
                    switch (role)
                    {
                        // TODO test 0 size array
                        case "READ":
                            Readers = addresses.Select(address => new Uri(address)).ToArray();
                            break;
                        case "WRITE":
                            Writers = addresses.Select(address => new Uri(address)).ToArray();
                            break;
                        case "ROUTE":
                            Routers = addresses.Select(address => new Uri(address)).ToArray();
                            break;
                    }
                }
            }
        }
    }
}