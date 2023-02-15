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

public sealed class RxWriteCommandInTx : RxCommand
{
    private readonly StressTest _test;

    public RxWriteCommandInTx(StressTest test, IDriver driver, bool useBookmark)
        : base(driver, useBookmark)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
    }

    public override async Task ExecuteAsync(StressTestContext context)
    {
        var session = NewSession(AccessMode.Write, context);

        await BeginTransaction(session, context)
            .SelectMany(
                txc => txc
                    .Run("CREATE ()")
                    .Consume()
                    .Catch(
                        (Exception error) =>
                            !_test.HandleWriteFailure(error, context)
                                ? txc.Rollback<IResultSummary>().Concat(Observable.Throw<IResultSummary>(error))
                                : txc.Rollback<IResultSummary>())
                    .Concat(txc.Commit<IResultSummary>())
                    .Select(
                        summary =>
                        {
                            summary.Counters.NodesCreated.Should().Be(1);
                            context.NodeCreated();
                            return summary;
                        })
                    .Finally(
                        () =>
                        {
                            if (session.LastBookmarks != null)
                            {
                                context.Bookmarks = session.LastBookmarks;
                            }
                        }))
            .SingleOrDefaultAsync()
            .CatchAndThrow(_ => session.Close<IResultSummary>())
            .Concat(session.Close<IResultSummary>());
    }
}
