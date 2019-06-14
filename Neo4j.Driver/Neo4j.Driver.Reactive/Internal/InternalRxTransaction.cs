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
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal class InternalRxTransaction : IRxTransaction
    {
        private readonly ITransaction _transaction;

        public InternalRxTransaction(ITransaction transaction)
        {
            _transaction = transaction;
        }

        #region Run Methods

        public IRxResult Run(string statement)
        {
            return Run(statement, null);
        }

        public IRxResult Run(string statement, object parameters)
        {
            return Run(new Statement(statement, parameters.ToDictionary()));
        }

        public IRxResult Run(Statement statement)
        {
            return new InternalRxResult(Observable.FromAsync(() => _transaction.RunAsync(statement)));
        }

        #endregion

        public IObservable<T> Commit<T>()
        {
            return Observable.FromAsync(() => _transaction.CommitAsync()).SelectMany(x => Observable.Empty<T>());
        }

        public IObservable<T> Rollback<T>()
        {
            return Observable.FromAsync(() => _transaction.RollbackAsync()).SelectMany(x => Observable.Empty<T>());
        }
    }
}