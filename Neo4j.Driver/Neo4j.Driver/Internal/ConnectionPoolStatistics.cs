using System;
using System.Collections.Generic;
using System.Threading;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPoolStatistics : IStatisticsProvider, IDisposable
    {
        public int InUseConns => _pool?.NumberOfInUseConnections ?? _inUseConns;
        public int AvailableConns => _pool?.NumberOfAvailableConnections ?? _availableConns;
        public long ConnCreated => _connCreated;
        public long ConnClosed => _connClosed;
        public long ConnToCreate => _connToCreate;
        public long ConnFailedToCreate => _connFailedToCreate;
        public long ConnToClose => _connToClose;

        public string PoolStatus => _pool != null ? InUsePool : _poolStatus;

        private long _connCreated;
        private long _connClosed;
        private long _connToCreate;
        private long _connFailedToCreate;
        private long _connToClose;

        private readonly int _inUseConns = 0;
        private readonly int _availableConns = 0;
        private readonly string _poolStatus = DisposedPool;

        private const string DisposedPool = "Disposed";
        private const string InUsePool = "InUse";

        private ConnectionPool _pool;
        private readonly string _name;

        public ConnectionPoolStatistics(Uri uri, ConnectionPool pool)
        {
            _name = $"{GetType().Name}[{uri}]({Guid.NewGuid()})";
            _pool = pool;
        }

        private ConnectionPoolStatistics(string name, IDictionary<string, object> statistics)
        {
            _name = name;

            _connCreated = statistics[nameof(ConnCreated)].As<long>();
            _connClosed = statistics[nameof(ConnClosed)].As<long>();
            _connToCreate = statistics[nameof(ConnToCreate)].As<long>();
            _connFailedToCreate = statistics[nameof(ConnFailedToCreate)].As<long>();
            _connToClose = statistics[nameof(ConnToClose)].As<long>();

            _inUseConns = statistics[nameof(InUseConns)].As<int>();
            _availableConns = statistics[nameof(AvailableConns)].As<int>();
            _poolStatus = statistics[nameof(PoolStatus)].ToString();
        }

        public void IncrementConnectionCreated()
        {
            Interlocked.Increment(ref _connCreated);
        }

        public void IncrementConnectionClosed()
        {
            Interlocked.Increment(ref _connClosed);
        }

        public void IncrementConnectionToCreate()
        {
            Interlocked.Increment(ref _connToCreate);
        }

        public void IncrementConnectionFailedToCreate()
        {
            Interlocked.Increment(ref _connFailedToCreate);
        }

        public void IncrementConnectionToClose()
        {
            Interlocked.Increment(ref _connToClose);
        }

        public string GetUniqueName()
        {
            return _name;
        }

        public IDictionary<string, object> ReportStatistics()
        {
            return this.ToDictionary();
        }

        public void Dispose()
        {
            _pool = null;
        }

        public static ConnectionPoolStatistics Read(string name, IDictionary<string, object> dict) 
            => new ConnectionPoolStatistics(name, dict);
    }
}