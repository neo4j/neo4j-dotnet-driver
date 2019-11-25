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
    internal class InternalTransaction : ITransaction
    {
        private readonly IInternalAsyncTransaction _txc;
        private readonly BlockingExecutor _executor;

        public InternalTransaction(IInternalAsyncTransaction txc, BlockingExecutor executor)
        {
            _txc = txc ?? throw new ArgumentNullException(nameof(txc));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public bool IsOpen => _txc.IsOpen;

        public IResult Run(string query)
        {
            return Run(new Query(query));
        }

        public IResult Run(string query, object parameters)
        {
            return Run(new Query(query, parameters.ToDictionary()));
        }

        public IResult Run(string query, IDictionary<string, object> parameters)
        {
            return Run(new Query(query, parameters));
        }

        public IResult Run(Query query)
        {
            return new InternalResult(_executor.RunSync(() => _txc.RunAsync(query)), _executor);
        }

        public void Commit()
        {
            _executor.RunSync(() => _txc.CommitAsync());
        }

        public void Rollback()
        {
            _executor.RunSync(() => _txc.RollbackAsync());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && IsOpen)
            {
                Rollback();
            }
        }
    }
}