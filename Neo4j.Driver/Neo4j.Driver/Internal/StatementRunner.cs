// Copyright (c) "Neo4j"
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal abstract class StatementRunner : IStatementRunner
    {
        public abstract IStatementResult Run(Statement statement);

        public abstract Task<IStatementResultCursor> RunAsync(Statement statement);

        public IStatementResult Run(string statement)
        {
            return Run(new Statement(statement));
        }

        public Task<IStatementResultCursor> RunAsync(string statement)
        {
            return RunAsync(new Statement(statement));
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters)
        {
            return Run(new Statement(statement, parameters));
        }

        public Task<IStatementResultCursor> RunAsync(string statement, IDictionary<string, object> parameters)
        {
            return RunAsync(new Statement(statement, parameters));
        }

        public IStatementResult Run(string statement, object parameters)
        {
            var cypherStatement = new Statement(statement, parameters.ToDictionary());
            return Run(cypherStatement);
        }

        public Task<IStatementResultCursor> RunAsync(string statement, object parameters)
        {
            return RunAsync(new Statement(statement, parameters.ToDictionary()));
        }

        protected abstract void Dispose(bool isDisposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    internal interface IResultResourceHandler
    {
        void OnResultConsumed(Bookmark bookmark);
        Task OnResultConsumedAsync(Bookmark bookmark);
    }

    internal interface ITransactionResourceHandler
    {
        void OnTransactionDispose(Bookmark bookmark);
        Task OnTransactionDisposeAsync(Bookmark bookmark);
    }
}
