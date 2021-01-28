﻿// Copyright (c) "Neo4j"
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
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal static class TaskHelper
    {
#if NET452
        private static readonly Task completedTask = Task.WhenAll();
#endif

        public static Task GetCompletedTask()
        {
#if NET452
            return completedTask;
#else
            return Task.CompletedTask;
#endif
        }

        public static Task GetFailedTask(Exception exc)
        {
#if NET452
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.SetException(exc);
            return tcs.Task;
#else
            return Task.FromException(exc);
#endif
        }
        
    }
}