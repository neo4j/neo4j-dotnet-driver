// Copyright (c) "Neo4j"
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

#nullable enable

using System;

namespace Neo4j.Driver.Internal;

/// <summary>
/// Structured logger for Query API components. Modelled on Microsoft.Extensions.Logging.ILogger
/// but limited to the level methods used in this package.
/// </summary>
internal interface ILogger
{
    void Trace(string messageTemplate, params object?[] args);
    void Debug(string messageTemplate, params object?[] args);
    void Info(string messageTemplate, params object?[] args);
    void Warn(string messageTemplate, params object?[] args);
    void Warn(Exception exception, string messageTemplate, params object?[] args);
    void Error(string messageTemplate, params object?[] args);
    void Error(Exception exception, string messageTemplate, params object?[] args);
}
