﻿// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Util
{
    internal class CancellationTokenWaitWrapper : IDisposable
    {
        private readonly CancellationToken _token;
        private readonly int _pollMs;
        private readonly CancellationTokenSource _linkedSource;

        public CancellationTokenWaitWrapper(CancellationToken token, int pollMs = 100)
        {
            _token = token;
            _pollMs = pollMs;
            _linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        public async Task RunDelayAsync()
        {
            try
            {
                while (!_linkedSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(_pollMs, _linkedSource.Token).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            _token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _linkedSource.Cancel();
        }
    }
}