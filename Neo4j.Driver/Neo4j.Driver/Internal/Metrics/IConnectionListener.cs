﻿// Copyright (c) "Neo4j"
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
using System.Diagnostics;

namespace Neo4j.Driver.Internal.Metrics
{
    internal interface IConnectionListener
    {
        void ConnectionConnecting(IListenerEvent connEvent);
        void ConnectionConnected(IListenerEvent connEvent);

        void ConnectionAcquired(IListenerEvent connEvent);
        void ConnectionReleased(IListenerEvent connEvent);
    }

    internal interface IListenerEvent
    {
        void Start();
        long GetElapsed();
    }

    /// <summary>
    /// A very simple impl of <see cref="IListenerEvent"/> without much error checks.
    /// </summary>
    internal class SimpleTimerEvent : IListenerEvent
    {
        private long _startTimestamp;

        public void Start()
        {
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public long GetElapsed()
        {
            return Stopwatch.GetTimestamp() - _startTimestamp;
        }
    }
}
