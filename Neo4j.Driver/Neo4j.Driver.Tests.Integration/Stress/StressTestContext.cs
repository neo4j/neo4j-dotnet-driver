// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.IntegrationTests.Stress;

public abstract class StressTestContext
{
    private long _bookmarkFailures;
    private Bookmarks _bookmarks;
    private long _createdNodesCount;
    private long _readNodesCount;
    private bool _stopped;

    public bool Stopped => Volatile.Read(ref _stopped);

    public Bookmarks Bookmarks
    {
        get => Volatile.Read(ref _bookmarks);
        set => Volatile.Write(ref _bookmarks, value);
    }

    public long BookmarkFailures => Interlocked.Read(ref _bookmarkFailures);

    public long ReadNodesCount => Interlocked.Read(ref _readNodesCount);

    public long CreatedNodesCount => Interlocked.Read(ref _createdNodesCount);

    public void Stop()
    {
        Volatile.Write(ref _stopped, true);
    }

    public void BookmarkFailed()
    {
        Interlocked.Increment(ref _bookmarkFailures);
    }

    public void NodeRead(IResultSummary summary)
    {
        Interlocked.Increment(ref _readNodesCount);
        ProcessSummary(summary);
    }

    public void NodeCreated()
    {
        Interlocked.Increment(ref _createdNodesCount);
    }

    protected abstract void ProcessSummary(IResultSummary summary);
}
