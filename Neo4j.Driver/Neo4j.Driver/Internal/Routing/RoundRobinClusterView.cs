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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoundRobinClusterView
    {
        private const int MinRouterCount = 1;
        private readonly ConcurrentRoundRobinSet<Uri> _routers = new ConcurrentRoundRobinSet<Uri>();
        private readonly ConcurrentRoundRobinSet<Uri> _detachedRouters = new ConcurrentRoundRobinSet<Uri>();
        private readonly ConcurrentRoundRobinSet<Uri> _readers = new ConcurrentRoundRobinSet<Uri>();
        private readonly ConcurrentRoundRobinSet<Uri> _writers = new ConcurrentRoundRobinSet<Uri>();

        public RoundRobinClusterView(Uri seed = null)
        {
            if (seed != null)
            {
                _routers.Add(seed);
            }
        }

        public RoundRobinClusterView(IEnumerable<Uri> routers, IEnumerable<Uri> readers, IEnumerable<Uri> writers)
        {
            _routers.Add(routers);
            _readers.Add(readers);
            _writers.Add(writers);
        }

        public bool IsStale()
        {
            return
//                expires < clock.millis() ||
                _routers.Count <= MinRouterCount || _readers.IsEmpty || _writers.IsEmpty;
        }

        public bool TryNextRouter(out Uri uri)
        {
            return _routers.TryNext(out uri);
        }

        public bool TryNextReader(out Uri uri)
        {
            return _readers.TryNext(out uri);
        }

        public bool TryNextWriter(out Uri uri)
        {
            return _writers.TryNext(out uri);
        }

        public void Remove(Uri uri)
        {
            _routers.Remove(uri);
            _detachedRouters.Add(uri);
            _readers.Remove(uri);
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
    }
}