// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Neo4j.Driver;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoutingTable : IRoutingTable
    {
        private const int MinRouterCount = 1;
        private readonly ConcurrentOrderedSet<Uri> _routers = new ConcurrentOrderedSet<Uri>();
        private readonly ConcurrentOrderedSet<Uri> _readers = new ConcurrentOrderedSet<Uri>();
        private readonly ConcurrentOrderedSet<Uri> _writers = new ConcurrentOrderedSet<Uri>();
        private readonly string _database;
        DateTime _expirationTimeStamp;

        public string Database => _database;
        public IList<Uri> Routers => _routers.Snapshot;
        public IList<Uri> Readers => _readers.Snapshot;
        public IList<Uri> Writers => _writers.Snapshot;
        public long ExpireAfterSeconds { get; }


        public RoutingTable(string database, IEnumerable<Uri> routers, long expireAfterSeconds = 0)
            : this(database, routers, Enumerable.Empty<Uri>(), Enumerable.Empty<Uri>(), expireAfterSeconds)
        {
        }

        public RoutingTable(string database, IEnumerable<Uri> routers, IEnumerable<Uri> readers,
            IEnumerable<Uri> writers, long expireAfterSeconds)
        {
            _database = database ?? string.Empty;

            _routers.Add(routers ?? Enumerable.Empty<Uri>());
            _readers.Add(readers ?? Enumerable.Empty<Uri>());
            _writers.Add(writers ?? Enumerable.Empty<Uri>());

            ExpireAfterSeconds = expireAfterSeconds;
            try
            {
                _expirationTimeStamp = DateTime.Now.AddSeconds(Convert.ToDouble(expireAfterSeconds));
            }
            catch
            {
                throw new ArgumentOutOfRangeException("Trying to set a TTL value for the routing table that is too large");
            }
            
        }

        public bool IsStale(AccessMode mode)
        {
            return _routers.Count < MinRouterCount
                   || mode == AccessMode.Read && _readers.IsEmpty
                   || mode == AccessMode.Write && _writers.IsEmpty
                   || DateTime.Now >= _expirationTimeStamp;
        }

        public bool IsExpiredFor(TimeSpan duration)
        { 
            return  (DateTime.Now - _expirationTimeStamp).TotalMilliseconds >= duration.TotalMilliseconds;
        }

        public bool IsReadingInAbsenceOfWriter(AccessMode mode)
        {
            return mode == AccessMode.Read && !IsStale(AccessMode.Read) && IsStale(AccessMode.Write);
        }

        public void Remove(Uri uri)
        {
            _routers.Remove(uri);
            _readers.Remove(uri);
            _writers.Remove(uri);
        }

        public void RemoveWriter(Uri uri)
        {
            _writers.Remove(uri);
        }

        public IEnumerable<Uri> All()
        {
            var all = new HashSet<Uri>();
            all.UnionWith(_routers);
            all.UnionWith(_readers);
            all.UnionWith(_writers);
            return all;
        }

        public override string ToString()
        {
            return new StringBuilder(128)
                .Append("RoutingTable{")
                .AppendFormat("database={0}, ", string.IsNullOrEmpty(_database) ? "default database" : _database)
                .AppendFormat("routers=[{0}], ", _routers)
                .AppendFormat("writers=[{0}], ", _writers)
                .AppendFormat("readers=[{0}], ", _readers)
                .AppendFormat("expiresAfter={0}s", ExpireAfterSeconds)
                .Append("}")
                .ToString();
        }

        public void PrependRouters(IEnumerable<Uri> uris)
        {
            var existing = _routers.ToList();
            _routers.Clear();
            _routers.Add(uris);
            _routers.Add(existing);
        }
    }
}