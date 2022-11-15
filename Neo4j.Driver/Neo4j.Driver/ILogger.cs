// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

namespace Neo4j.Driver;

/// <summary>
/// The new <see cref="ILogger" /> differs from the legacy one in the message format the logging methods are accepting. In
/// <see cref="ILogger" />, each logging method accepts a message which specifies how the message would be formatted and
/// one or many arguments that are used to replace placeholders in the message string. The following example shows a
/// simplified version of how the <see cref="ILogger" /> is used in this driver:
/// <code>
/// logger.Info("Hello {0}, {1}", "Alice", "Bob");
/// </code>
/// </summary>
public interface ILogger
{
    /// <summary>Logs an error.</summary>
    /// <param name="cause">The <see cref="Exception" /> that causes the error. This value could be null if not applied.</param>
    /// <param name="message">The message of the error. This value could be null if not applied.</param>
    /// <param name="args">The arguments to replace placeholders in the message string.</param>
    void Error(Exception cause, string message, params object[] args);

    /// <summary>Logs a warning.</summary>
    /// <param name="cause">Any <see cref="Exception" /> that causes this warning. This value could be null if not applied.</param>
    /// <param name="message">The message of the warning. This value could be null if not applied.</param>
    /// <param name="args">The arguments to replace placeholders in the message string.</param>
    void Warn(Exception cause, string message, params object[] args);

    /// <summary>Logs an information message.</summary>
    /// <param name="message">The message of the information.</param>
    /// <param name="args">The arguments to replace placeholders in the message.</param>
    void Info(string message, params object[] args);

    /// <summary>
    /// Logs useful messages for debugging. The Bolt messages sent and received by this driver are logged at this
    /// level.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="args">The arguments to replace placeholders in the message.</param>
    void Debug(string message, params object[] args);

    /// <summary>Log useful messages for tracing. The Bolt messages sent and received in hex binary are logged at this level.</summary>
    /// <param name="message">The message.</param>
    /// <param name="args">The arguments to replace placeholders in the message.</param>
    void Trace(string message, params object[] args);

    /// <summary>Return if trace logging level is enabled.</summary>
    /// <returns>True if trace logging level is enabled, otherwise False.</returns>
    bool IsTraceEnabled();

    /// <summary>Return if debug logging level is enabled.</summary>
    /// <returns>True if debug logging level is enabled, otherwise False.</returns>
    bool IsDebugEnabled();
}
