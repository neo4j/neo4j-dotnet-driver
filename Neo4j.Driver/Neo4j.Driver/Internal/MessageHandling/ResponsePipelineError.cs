﻿// Copyright (c) "Neo4j"
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
using System.Diagnostics.CodeAnalysis;

namespace Neo4j.Driver.Internal.MessageHandling;

// we sometimes access the _thrown field without locking, but it's fine because it's only a volatile bool
[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
internal sealed class ResponsePipelineError : IResponsePipelineError
{
    private volatile bool _thrown;

    public ResponsePipelineError(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _thrown = false;
    }

    public Exception Exception { get; }

    public void EnsureThrown()
    {
        if (_thrown)
        {
            return;
        }

        lock (this)
        {
            if (_thrown)
            {
                return;
            }

            _thrown = true;
            throw Exception;
        }
    }

    public void EnsureThrownIf<T>()
    {
        if (_thrown || Exception is not T)
        {
            return;
        }

        lock (this)
        {
            if (_thrown)
            {
                return;
            }

            _thrown = true;
            throw Exception;
        }
    }
}
