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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Logging
{
    /// <inheritdoc />
    /// <summary>
    /// The way we delegate to legacy <see cref="T:Neo4j.Driver.V1.ILogger" /> is by using message string only.
    /// All message format string and arguments will first be converted to message string and
    /// then be passed down to <see cref="T:Neo4j.Driver.V1.ILogger" /> without extra arguments.
    /// </summary>
    internal class LegacyLoggerAdapter : IDriverLogger
    {
        private readonly ILogger _logger;
        public LegacyLoggerAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public void Error(Exception cause, string message = "", params object[] args)
        {
            if (cause != null)
            {
                _logger?.Error(string.Format(message, args), cause);
            }
            else
            {
                _logger?.Error(string.Format(message, args));
            }
        }

        public void Warn(Exception cause, string message = "", params object[] args)
        {
            if (cause != null)
            {
                _logger?.Info(string.Format(message, args), cause);
            }
            else
            {
                _logger?.Info(string.Format(message, args));
            }
        }

        public void Info(string message, params object[] args)
        {
            _logger?.Info(string.Format(message, args));
        }

        public void Debug(string message, params object[] args)
        {
            _logger?.Debug(string.Format(message, args));
        }

        public void Trace(string message, params object[] args)
        {
            _logger?.Trace(string.Format(message, args));
        }

        public bool IsTraceEnabled()
        {
            return _logger != null && _logger.Level >= LogLevel.Trace;
        }

        public bool IsDebugEnabled()
        {
            return _logger != null && _logger.Level >= LogLevel.Debug;
        }
    }
}