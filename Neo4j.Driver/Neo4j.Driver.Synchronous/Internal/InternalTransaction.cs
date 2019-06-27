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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    internal class InternalTransaction : ISyncTransaction
    {
        private readonly IReactiveTransaction _txc;
        private readonly SyncExecutor _syncExecutor;

        public InternalTransaction(IReactiveTransaction txc, SyncExecutor syncExecutor)
        {
            _txc = txc ?? throw new ArgumentNullException(nameof(txc));
            _syncExecutor = syncExecutor ?? throw new ArgumentNullException(nameof(syncExecutor));
        }

        public IStatementResult Run(string statement)
        {
            return Run(new Statement(statement));
        }

        public IStatementResult Run(string statement, object parameters)
        {
            return Run(new Statement(statement, parameters.ToDictionary()));
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters)
        {
            return Run(new Statement(statement, parameters));
        }

        public IStatementResult Run(Statement statement)
        {
            return new InternalStatementResult(_syncExecutor.RunSync(() => _txc.RunAsync(statement)), _syncExecutor);
        }

        public void Success()
        {
            _txc.Success();
        }

        public void Failure()
        {
            _txc.Failure();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _syncExecutor.RunSync(() => _txc.CloseAsync());
            }
        }
    }
}