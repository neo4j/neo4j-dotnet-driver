// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Logging
{
    internal abstract class ReformattedLogger : IDriverLogger
    {
        private readonly IDriverLogger _delegate;

        protected ReformattedLogger(IDriverLogger logger)
        {
            _delegate = logger;
        }

        protected abstract string Reformat(string message);

        public void Error(Exception cause, string message, params object[] args)
        {
            _delegate?.Error(cause, Reformat(message), args);
        }

        public void Warn(Exception cause, string message, params object[] args)
        {
            _delegate?.Warn(cause, Reformat(message), args);
        }

        public void Info(string message, params object[] args)
        {
            _delegate?.Info(Reformat(message), args);
        }

        public void Debug(string message, params object[] args)
        {
            if (IsDebugEnabled())
            {
                _delegate?.Debug(Reformat(message), args);
            }
        }

        public void Trace(string message, params object[] args)
        {
            if (IsTraceEnabled())
            {
                _delegate?.Trace(Reformat(message), args);
            }
        }

        public bool IsTraceEnabled()
        {
            return _delegate!= null && _delegate.IsTraceEnabled();
        }

        public bool IsDebugEnabled()
        {
            return _delegate != null && _delegate.IsDebugEnabled();
        }
    }
}