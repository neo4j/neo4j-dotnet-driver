// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.IntegrationTests.Stress;

public class RxWriteCommand<TContext> : RxCommand<TContext>
    where TContext : StressTestContext
{
    private readonly StressTest<TContext> _test;

    public RxWriteCommand(StressTest<TContext> test, IDriver driver, bool useBookmark)
        : base(driver, useBookmark)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
    }

    public override async Task ExecuteAsync(TContext context)
    {
        var session = NewSession(AccessMode.Write, context);

        await session
            .Run("CREATE ()")
            .Consume()
            .Select(
                summary =>
                {
                    summary.Counters.NodesCreated.Should().Be(1);
                    context.NodeCreated();
                    return summary;
                })
            .Catch(
                (Exception error) =>
                    !_test.HandleWriteFailure(error, context)
                        ? Observable.Throw<IResultSummary>(error)
                        : Observable.Empty<IResultSummary>())
            .Finally(
                () =>
                {
                    if (session.LastBookmarks != null)
                    {
                        context.Bookmarks = session.LastBookmarks;
                    }
                })
            .SingleOrDefaultAsync()
            .CatchAndThrow(_ => session.Close<IResultSummary>())
            .Concat(session.Close<IResultSummary>());
    }
}
