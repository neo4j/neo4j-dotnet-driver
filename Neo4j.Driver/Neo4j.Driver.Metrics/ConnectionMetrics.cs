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

namespace Neo4j.Driver.Internal.Metrics
{
    internal class ConnectionMetrics : IConnectionMetrics, IConnectionListener
    {
        public string UniqueName { get; }
        public IHistogram ConnectionTimeHistogram => _connectionTimeHistogram.Snapshot();
        public IHistogram InUseTimeHistogram => _inUseTimeHistogram.Snapshot();

        private readonly Histogram _connectionTimeHistogram;
        private readonly Histogram _inUseTimeHistogram;

        public ConnectionMetrics(Uri uri, TimeSpan connectionTimeout)
        {
            UniqueName = uri.ToString();
            _connectionTimeHistogram = new Histogram(connectionTimeout.Ticks);
            _inUseTimeHistogram = new Histogram();
        }

        public void ConnectionConnecting(IListenerEvent connEvent)
        {
            connEvent.Start();
        }

        public void ConnectionConnected(IListenerEvent connEvent)
        {
            _connectionTimeHistogram.RecordValue(connEvent.GetElapsed());
        }

        public void ConnectionAcquired(IListenerEvent connEvent)
        {
            connEvent.Start();
        }

        public void ConnectionReleased(IListenerEvent connEvent)
        {
            _inUseTimeHistogram.RecordValue(connEvent.GetElapsed());
        }

        public override string ToString()
        {
            return this.ToDictionary().ToContentString();
        }
    }

}
