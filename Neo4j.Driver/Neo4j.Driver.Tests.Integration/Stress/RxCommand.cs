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
using Microsoft.Reactive.Testing;

namespace Neo4j.Driver.IntegrationTests.Stress;

public abstract class RxCommand : ReactiveTest, IRxCommand
{
    private readonly IDriver _driver;
    private readonly bool _useBookmark;

    protected RxCommand(IDriver driver, bool useBookmark)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _useBookmark = useBookmark;
    }

    public abstract Task ExecuteAsync(StressTestContext context);

    protected IRxSession NewSession(AccessMode mode, StressTestContext context)
    {
        return _driver.RxSession(
            o =>
                o.WithDefaultAccessMode(mode)
                    .WithBookmarks(_useBookmark ? new[] { context.Bookmarks } : Array.Empty<Bookmarks>()));
    }

    protected IObservable<IRxTransaction> BeginTransaction(IRxSession session, StressTestContext context)
    {
        if (_useBookmark)
        {
            return session.BeginTransaction()
                .RetryWhen(
                    failed =>
                    {
                        return failed.SelectMany(
                            exc =>
                            {
                                if (exc is TransientException)
                                {
                                    context.BookmarkFailed();
                                    return Observable.Return(1);
                                }

                                return Observable.Throw<int>(exc);
                            });
                    });
        }

        return session.BeginTransaction();
    }
}
