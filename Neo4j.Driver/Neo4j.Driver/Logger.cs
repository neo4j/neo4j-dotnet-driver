﻿//  Copyright (c) 2002-2016 "Neo Technology,"
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
    /// <summary>
    /// The logging levels that could be used by a logger.
    /// </summary>
    public enum LogLevel
    {
        None,
        Error,
        Info,
        Debug,
        Trace
    }

    /// <summary>
    /// The logger used by this driver.
    /// </summary>
    /// <remarks>
    /// Set the logger that you want to use via <see cref="Config"/>.
    /// If no logger is explicitly set, then a default debug logger would be used <see cref="Config.DefaultConfig"/></remarks>
    public interface ILogger 
    {
        /// <summary>Log a message at <see cref="LogLevel.Error"/> level.</summary>
        /// <param name="message">The error message.</param>
        /// <param name="cause">The cause of the error.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message.</param>
        void Error(string message, Exception cause = null, params object[] restOfMessage);

        /// <summary>Log a message at <see cref="LogLevel.Info"/> level.</summary>
        /// <param name="message">The message.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message.</param>
        void Info(string message, params object[] restOfMessage);

        /// <summary>Log a message at <see cref="LogLevel.Debug"/> level.</summary>
        /// <param name="message">The message.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message, including (but not limited to) Sent Messages and Received Messages.</param>
        void Debug(string message, params object[] restOfMessage);

        /// <summary>Log a message at <see cref="LogLevel.Trace"/> level</summary>
        /// <param name="message">The message.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message, including (but not limited to) bytes sent and received over the connection.</param>
        void Trace(string message, params object[] restOfMessage);

        /// <summary>
        /// Gets and sets the level of this <see cref="ILogger"/>
        /// </summary>
        LogLevel Level { get; set; }
    }
}
