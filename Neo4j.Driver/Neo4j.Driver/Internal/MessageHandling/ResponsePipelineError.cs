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

namespace Neo4j.Driver.Internal.MessageHandling
{
    internal class ResponsePipelineError : IResponsePipelineError
    {
        private readonly Exception _exception;
        private volatile bool _thrown;

        public ResponsePipelineError(Exception exception)
        {
            _exception = exception ?? throw new ArgumentNullException(nameof(exception));
            _thrown = false;
        }

        public bool Is<T>()
        {
            return _exception is T;
        }

        public bool Is(Func<Exception, bool> predicate)
        {
            return predicate(_exception);
        }

        public void EnsureThrown()
        {
            EnsureThrownIf(e => true);
        }

        public void EnsureThrownIf<T>()
        {
            EnsureThrownIf(e => e is T);
        }

        public void EnsureThrownIf(Func<Exception, bool> predicate)
        {
            if (_thrown || !Is(predicate)) return;

            lock (this)
            {
                if (!_thrown)
                {
                    _thrown = true;
                    throw _exception;
                }
            }
        }
    }
}