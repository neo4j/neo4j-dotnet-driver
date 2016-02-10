//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;

namespace Neo4j.Driver
{
    public enum LogLevel
    {
        None,
        Error,
        Info,
        Debug,
        Trace
    }

    public interface ILogger : IDisposable
    {
        void Error(string message, Exception cause = null, params object[] restOfMessage);

        /// <summary>Log a message at info level.</summary>
        /// <param name="message">The message.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message, including (but not limited to) Sent Messages and Received Messages.</param>
        void Info(string message, params object[] restOfMessage);

        void Debug(string message, params object[] restOfMessage);

        void Trace(string message, params object[] restOfMessage);

        LogLevel Level { get; set; }
    }
}
