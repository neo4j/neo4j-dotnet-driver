// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal class BlockingExecutor
{
    private readonly TaskFactory _taskFactory;

    public BlockingExecutor()
        : this(TaskScheduler.Default)
    {
    }

    public BlockingExecutor(TaskScheduler scheduler)
    {
        scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            scheduler);
    }

    public void RunSync(Func<Task> task)
    {
        _taskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
    }

    public T RunSync<T>(Func<Task<T>> task)
    {
        return _taskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();
    }
}
