using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> SingleAsync<T>(this Task<IResultCursor<T>> cursorTask)
        {
            var cursor = await cursorTask.ConfigureAwait(false);
            return await cursor.SingleAsync().ConfigureAwait(false);
        }

        public static async Task<List<T>> ToListAsync<T>(this Task<IResultCursor<T>> cursorTask)
        {
            var cursor = await cursorTask.ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        public static async Task<IResultSummary> ForEachAsync<T>(this Task<IResultCursor<T>> cursorTask,
            Func<T, Task> operation)
        {
            var cursor = await cursorTask.ConfigureAwait(false);
            return await cursor.ForEachAsync(operation).ConfigureAwait(false);
        }

        public static async Task<IResultSummary> ForEachAsync<T>(this Task<IResultCursor<T>> cursorTask,
            Action<T> operation)
        {
            var cursor = await cursorTask.ConfigureAwait(false);
            return await cursor.ForEachAsync(operation).ConfigureAwait(false);
        }
    }
}
