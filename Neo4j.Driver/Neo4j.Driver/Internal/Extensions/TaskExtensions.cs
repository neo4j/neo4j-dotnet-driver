// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal static class TaskExtensions
{
    public static async Task Timeout(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var delay = Task.Delay(timeout, linkedSource.Token);
            var finished = await Task.WhenAny(task, delay).ConfigureAwait(false);

            if (finished.IsCanceled)
            {
                throw new TaskCanceledException(task);
            }

            if (finished.IsCompleted && finished == delay)
            {
                throw new TimeoutException();
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

            if (finished.IsCanceled)
            {
                throw new TaskCanceledException(task);
            }

            if (finished.IsCompleted && finished == delay)
            {
                throw new TimeoutException();
            }

            return await task.ConfigureAwait(false);
        }
        finally
        {
            linkedSource.Cancel();
        }
    }
}
