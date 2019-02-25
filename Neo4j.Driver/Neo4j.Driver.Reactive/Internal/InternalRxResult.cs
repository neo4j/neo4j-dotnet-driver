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
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal class InternalRxResult : IRxResult
    {
        private readonly IObservable<IStatementResultCursor> _resultCursor;

        public InternalRxResult(IObservable<IStatementResultCursor> resultCursor)
        {
            _resultCursor = resultCursor.Publish().AutoConnect().Replay().AutoConnect();
        }

        public IObservable<string[]> Keys()
        {
            return _resultCursor.SelectMany(r => r.KeysAsync());
        }

        public IObservable<IRecord> Records()
        {
            return Observable.Create(async (IObserver<IRecord> o) =>
            {
                var cts = new CancellationTokenSource();

                try
                {
                    var cursor = await _resultCursor.GetAwaiter();

                    while (await cursor.FetchAsync() && !cts.IsCancellationRequested)
                    {
                        o.OnNext(cursor.Current);
                    }

                    o.OnCompleted();
                }
                catch (Exception exc)
                {
                    o.OnError(exc);
                }

                return new CancellationDisposable(cts);
            });
        }

        public IObservable<IResultSummary> Summary()
        {
            return _resultCursor.SelectMany(r => r.ConsumeAsync());
        }
    }
}