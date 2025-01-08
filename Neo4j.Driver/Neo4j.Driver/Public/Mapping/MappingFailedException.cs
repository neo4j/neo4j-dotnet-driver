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

using System;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// The exception that is thrown when the mapping of a record to a target type failed.
/// </summary>
public class MappingFailedException : Neo4jException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingFailedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MappingFailedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingFailedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MappingFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
