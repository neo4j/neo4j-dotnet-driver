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
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    public abstract class AsyncCommand<TContext> : IAsyncCommand<TContext>
        where TContext : StressTestContext
    {
        protected readonly IDriver Driver;
        protected readonly bool UseBookmark;

        protected AsyncCommand(IDriver driver, bool useBookmark)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            UseBookmark = useBookmark;
        }

        public IAsyncSession NewSession(AccessMode mode, TContext context)
        {
            var bookmarks = UseBookmark 
                ? new[] {context.Bookmark}
                : Array.Empty<Bookmark>();
            
            return Driver.AsyncSession(o => o.WithDefaultAccessMode(mode).WithBookmarks(bookmarks));
        }

        public async Task<IAsyncTransaction> BeginTransaction(IAsyncSession session, TContext context)
        {
            if (!UseBookmark)
            {
                return await session.BeginTransactionAsync();
            }
            
            while (true)
            {
                try
                {
                    return await session.BeginTransactionAsync();
                }
                catch (TransientException)
                {
                    context.BookmarkFailed();
                }
            }
        }

        public abstract Task ExecuteAsync(TContext context);
    }
}