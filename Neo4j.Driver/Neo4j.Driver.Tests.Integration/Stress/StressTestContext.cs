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

using System.Threading;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public abstract class StressTestContext
    {
        private bool _stopped;
        private Bookmark _bookmark;
        private long _readNodesCount;
        private long _createdNodesCount;
        private long _bookmarkFailures;

        public bool Stopped => Volatile.Read(ref _stopped);

        public void Stop()
        {
            Volatile.Write(ref _stopped, true);
        }

        public Bookmark Bookmark
        {
            get => Volatile.Read(ref _bookmark);
            set => Volatile.Write(ref _bookmark, value);
        }

        public long BookmarkFailures => Interlocked.Read(ref _bookmarkFailures);

        public void BookmarkFailed()
        {
            Interlocked.Increment(ref _bookmarkFailures);
        }

        public long ReadNodesCount => Interlocked.Read(ref _readNodesCount);

        public void NodeRead(IResultSummary summary)
        {
            Interlocked.Increment(ref _readNodesCount);
            ProcessSummary(summary);
        }

        public long CreatedNodesCount => Interlocked.Read(ref _createdNodesCount);

        public void NodeCreated()
        {
            Interlocked.Increment(ref _createdNodesCount);
        }

        protected abstract void ProcessSummary(IResultSummary summary);
    }
}