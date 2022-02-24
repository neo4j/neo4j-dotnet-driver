﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Extensions
{
    internal static class TaskExtensions
    {
        public static async Task Timeout(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var delay = Task.Delay(timeout, linkedSource.Token);
                var finished = await Task.WhenAny(task, delay).ConfigureAwait(false);
                if (finished == delay)
                {
                    throw new TaskCanceledException(task);
                }

                await task.ConfigureAwait(false);
            }
            finally
            {
                linkedSource.Cancel();
            }
        }

        public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var delay = Task.Delay(timeout, linkedSource.Token);
                var finished = await Task.WhenAny(task, delay).ConfigureAwait(false);

                if (finished == delay)
                {
                    throw new TaskCanceledException(task);
                }

                return await task.ConfigureAwait(false);
            }
            finally
            {
                linkedSource.Cancel();
            }
        }
    }
}
