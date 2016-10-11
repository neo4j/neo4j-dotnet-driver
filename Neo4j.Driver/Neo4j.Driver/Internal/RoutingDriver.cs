// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class RoutingDriver : IDriver
    {
        private IClusterConnectionPool _connectionPool;
        private ILogger _logger;
        private ClusterView _clusterView;

        internal RoutingDriver(
            Uri seedServer, 
            IAuthToken authToken, 
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings, 
            ILogger logger)
        {
            Uri = seedServer;
            _logger = logger;
            _connectionPool = new ClusterConnectionPool(authToken, encryptionManager, poolSettings, logger);
            _clusterView = new ClusterView(seedServer);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Uri Uri { get; }
        public ISession Session()
        {
            return Session(AccessMode.Write);
        }

        public ISession Session(AccessMode mode)
        {
            CheckServer();
            switch (mode)
            {
                case AccessMode.Read:
                    var readerUri = _clusterView.NextReader();
                    return new Session(_connectionPool.Acquire(readerUri), _logger);
                case AccessMode.Write:
                    var writerUri = _clusterView.NextWriter();
                    return new Session(_connectionPool.Acquire(writerUri), _logger);
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }
        }

        private void CheckServer()
        {
            if (!_clusterView.IsStale())
            {
                return;
            }
            var oldCluster = _clusterView.All();
            var newView = NewClusterView();
            var newCluster = newView.All();

            oldCluster.ExceptWith(newCluster);
            foreach (var server in oldCluster)
            {
                _connectionPool.Purge(server);
            }
            _clusterView = newView;
        }

        private ClusterView NewClusterView()
        {
            throw new NotImplementedException();
        }
    }

    internal class ClusterView
    {
        private const int MinRouterCount = 1;
        private ConcurrentRoundRobinSet<Uri> _router;
        private ConcurrentRoundRobinSet<Uri> _reader;
        private ConcurrentRoundRobinSet<Uri> _writer;

        public ClusterView(Uri seed = null)
        {
            if (seed != null)
            {
                _router.Add(seed);
            }
        }

        public bool IsStale()
        {
            return
//                expires < clock.millis() ||
                _router.Count <= MinRouterCount || _reader.IsEmpty() || _writer.IsEmpty();
        }

        public Uri NextRouter()
        {
            return _router.Hop();
        }

        public Uri NextReader()
        {
            return _reader.Hop();
        }

        public Uri NextWriter()
        {
            return _writer.Hop();
        }

        public ISet<Uri> All()
        {
            var all = new HashSet<Uri>();
            all.UnionWith(_router);
            all.UnionWith(_reader);
            all.UnionWith(_writer);
            return all;
        }
    }

    internal class ConcurrentRoundRobinSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> _set;
        private IEnumerator<T> _enumerator;
        private const byte DummyValue = default(byte);

        public ConcurrentRoundRobinSet()
        {
            _set = new ConcurrentDictionary<T, byte>();
        }

        /// <summary>
        /// Get the next value in the set in a round robin manner.
        /// </summary>
        /// <returns>The next value in the set in a round robin manner.</returns>
        public T Hop()
        {
            if (_enumerator == null)
            {
                if (_set.IsEmpty)
                {
                    throw new InvalidOperationException("Nothing in the bag");
                }
                _enumerator = _set.Keys.GetEnumerator(); // TODO: test what will happen when I add or remove from the enumerator
            }
            if (!_enumerator.MoveNext())
            {
                _enumerator.Reset();
            }
            return _enumerator.Current;
        }

        /// <summary>
        /// Return if the set is empty.
        /// </summary>
        /// <returns>True if the set is empty otherwise false.</returns>
        public bool IsEmpty()
        {
            return _set.IsEmpty;
        }

        public void Add(T item)
        {
            _set.AddOrUpdate(item, DummyValue, (key, value) => value);
        }

        public void Clear()
        {
            _set.Clear();
        }

        public bool Contains(T item)
        {
            return _set.ContainsKey(item);
        }

        public bool Remove(T item)
        {
            byte value; // I do not care about this out value at all
            return _set.TryRemove(item, out value);
        }

        public int Count => _set.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _set.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}