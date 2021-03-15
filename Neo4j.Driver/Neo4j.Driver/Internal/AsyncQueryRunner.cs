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
using Neo4j.Driver;

namespace Neo4j.Driver.Internal
{
    internal abstract class AsyncQueryRunner : IAsyncQueryRunner
    {
        public abstract Task<IResultCursor> RunAsync(Query query);

        public Task<IResultCursor> RunAsync(string query)
        {
            return RunAsync(new Query(query));
        }

        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
        {
            return RunAsync(new Query(query, parameters));
        }

        public Task<IResultCursor> RunAsync(string query, object parameters)
        {
            return RunAsync(new Query(query, parameters.ToDictionary()));
        }


		private bool _disposed = false;

		~AsyncQueryRunner() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			//No resources to clean up
			if(disposing)
			{
				//Dispose of resources here				
			}

			//Set disposed resources to null

			_disposed = true;
		}

		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore();

			Dispose(disposing: false);
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsyncCore()
		{
			//Nothing to dispose of in this class. Methods required for derived classes and correct dispose pattern
			await Task.CompletedTask;
		}
	}

    internal interface IResultResourceHandler
    {
        Task OnResultConsumedAsync();
    }

    internal interface ITransactionResourceHandler
    {
        Task OnTransactionDisposeAsync(Bookmark bookmark);
    }
}