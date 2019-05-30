// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoutingTable : IRoutingTable
    {
        private const int MinRouterCount = 1;
        private readonly AddressSet<Uri> _routers = new AddressSet<Uri>();
        private readonly AddressSet<Uri> _readers = new AddressSet<Uri>();
        private readonly AddressSet<Uri> _writers = new AddressSet<Uri>();
        private readonly long _expireAfterSeconds;

        public IList<Uri> Routers => _routers.Snaphost;
        public IList<Uri> Readers => _readers.Snaphost;
        public IList<Uri> Writers => _writers.Snaphost;
        public long ExpireAfterSeconds => _expireAfterSeconds;

        private readonly Stopwatch _stopwatch;

        public RoutingTable(IEnumerable<Uri> routers, long expireAfterSeconds = 0)
        :this(routers, Enumerable.Empty<Uri>(), Enumerable.Empty<Uri>(), expireAfterSeconds)
        {
        }

        public RoutingTable(IEnumerable<Uri> routers, IEnumerable<Uri> readers, IEnumerable<Uri> writers,
            long expireAfterSeconds)
        { 
            _routers.Add(routers);
            _readers.Add(readers);
            _writers.Add(writers);

            _expireAfterSeconds = expireAfterSeconds;
            _stopwatch = new Stopwatch();
            _stopwatch.Restart();
        }

        public bool IsStale(AccessMode mode)
        {
            return _routers.Count < MinRouterCount
                || mode == AccessMode.Read && _readers.IsEmpty
                || mode == AccessMode.Write && _writers.IsEmpty
                || _expireAfterSeconds < _stopwatch.Elapsed.TotalSeconds;
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

        public ISet<Uri> All()
        {
            var all = new HashSet<Uri>();
            all.UnionWith(_routers);
            all.UnionWith(_readers);
            all.UnionWith(_writers);
            return all;
        }

        public void Clear()
        {
            _routers.Clear();
            _readers.Clear();
            _writers.Clear();
        }

        public override string ToString()
        {
            return $"[{nameof(_routers)}: {_routers}], " +
                   $"[{nameof(_readers)}: {_readers}], " +
                   $"[{nameof(_writers)}: {_writers}]";
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
