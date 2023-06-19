// Copyright (c) 2002-2023 "Neo4j,"
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
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class TestLogger : ILogger
    {
        private readonly ExtendedLogLevel _level;
        private readonly Action<string> _logMethod;

        public TestLogger(ITestOutputHelper output, ExtendedLogLevel level = ExtendedLogLevel.Info)
        {
            _level = level;
            _logMethod = output.WriteLine;
        }

        public TestLogger(Action<string> logMethod, ExtendedLogLevel level = ExtendedLogLevel.Info)
        {
            _level = level;
            _logMethod = logMethod;
        }

        public void Error(Exception cause, string message, params object[] args)
        {
            Log(ExtendedLogLevel.Error, cause, message, args);
        }

        public void Warn(Exception cause, string message, params object[] args)
        {
            Log(ExtendedLogLevel.Warn, cause, message, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(ExtendedLogLevel.Info, null, message, args);
        }

        public void Debug(string message, params object[] args)
        {
            if (IsDebugEnabled()) Log(ExtendedLogLevel.Debug, null, message, args);
        }

        public void Trace(string message, params object[] args)
        {
            if (IsTraceEnabled()) Log(ExtendedLogLevel.Trace, null, message, args);
        }

        public bool IsTraceEnabled()
        {
            return _level >= ExtendedLogLevel.Trace;
        }

        public bool IsDebugEnabled()
        {
            return _level >= ExtendedLogLevel.Debug;
        }

        private void Log(ExtendedLogLevel level, Exception cause, string message, params object[] args)
        {
            message ??= "";
            var formattableString = $"[{level}]:{string.Format(message, args)}";

            if (cause != null) formattableString = $"{formattableString}\n{cause}";

            _logMethod(formattableString);
        }

        public static ILogger Create(ITestOutputHelper output)
        {
            var logLevel = ExtendedLogLevel.Error;
            var logLevelStr = Environment.GetEnvironmentVariable("NEOLOGLEVEL");
            if (!string.IsNullOrEmpty(logLevelStr)) logLevel = Enum.Parse<ExtendedLogLevel>(logLevelStr, true);

            return new TestLogger(output, logLevel);
        }
    }

    public enum ExtendedLogLevel
    {
        None,
        Error,
        Warn,
        Info,
        Debug,
        Trace,
        All
    }
}
