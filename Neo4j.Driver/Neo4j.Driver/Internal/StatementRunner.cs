// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal abstract class StatementRunner : LoggerBase, IStatementRunner, IStatementRunnerAsync
    {
        protected StatementRunner(ILogger logger) : base(logger)
        {
        }

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
    }

    internal interface IResultResourceHandler
    {
        void OnResultConsumed();
        Task OnResultConsumedAsync();
    }

    internal interface ITransactionResourceHandler
    {
        void OnTransactionDispose();
        Task OnTransactionDisposeAsync();
    }
}