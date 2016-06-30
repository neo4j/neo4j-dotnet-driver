// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Collections;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{

    internal class DebugLogger : BaseOutLogger
    {
        public DebugLogger() :
            base(s => System.Diagnostics.Debug.WriteLine(s))
        { }
    }

    internal abstract class BaseOutLogger : ILogger
    {
        protected Action<string> LogMethod { get; set; }
        public LogLevel Level { get; set; }

        protected BaseOutLogger() { }
        protected BaseOutLogger(Action<string> logMethod)
        {
            LogMethod = logMethod;
        }

        private void Log(LogLevel level, string message, params object[] restOfMessage)
        {
            if (Level < level || level == LogLevel.None)
            {
                return;
            }

            if (level == LogLevel.Trace)
            {
                LogMethod($"[{level}] => {message}{ByteBufferToString(restOfMessage)}");
            }
            else
            {
                LogMethod($"[{level}] => {message}{ObjectToString(restOfMessage)}");
            }
        }

        private string ByteBufferToString(object[] restOfMessage)
        {
            Throw.ArgumentOutOfRangeException.IfValueLessThan(restOfMessage.Length, 3, nameof(restOfMessage.Length));
            var buffer = (byte[])restOfMessage[0];
            var offset = (int) restOfMessage[1];
            var count = (int) restOfMessage[2];
            return buffer.ToHexString(offset, count);
        }

        private string ObjectToString(object o)
        {
            if (o == null)
            {
                return "NULL";
            }
            if (o is string)
            {
                return o.ToString();
            }
            if (o is byte[])
            {
                return ((byte[])o).ToHexString();
            }

            if (o is IEnumerable)
            {
                return EnumerableToString(o as IEnumerable);
            }

            return o.ToString();
        }

        private string EnumerableToString(IEnumerable items)
        {
            var output = (from object item in items select ObjectToString(item)).ToList();
            return $"[{string.Join(", ", output)}]";
        }


        // ALL, SEVERE, INFO, DEBUG, 
        public void Error(string message, Exception cause = null, params object[] restOfMessage)
        {
            Log(LogLevel.Error, message, cause, restOfMessage);
        }

        public void Info(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Info, message, restOfMessage);
        }

        public void Debug(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Debug, message, restOfMessage);
        }

        public void Trace(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Trace, message, restOfMessage);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            //Do nothing
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}