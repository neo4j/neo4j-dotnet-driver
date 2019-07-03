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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public abstract class RxCommand<TContext> : ReactiveTest, IRxCommand<TContext>
        where TContext : StressTestContext
    {
        protected readonly IDriver _driver;
        protected readonly bool _useBookmark;

        protected RxCommand(IDriver driver, bool useBookmark)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _useBookmark = useBookmark;
        }

        public IRxSession NewSession(AccessMode mode, TContext context)
        {
            return _useBookmark ? _driver.RxSession(mode, new[] {context.Bookmark}) : _driver.RxSession(mode);
        }

        public IObservable<IRxTransaction> BeginTransaction(IRxSession session, TContext context)
        {
            if (_useBookmark)
            {
                return session.BeginTransaction().RetryWhen(failed =>
                {
                    return failed.SelectMany(exc =>
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

        public abstract Task ExecuteAsync(TContext context);
    }
}