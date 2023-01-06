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
/// Use this attribute to decorate an exception class to declare that the class
/// is the correct class to create when an error with the specified code is raised.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class Neo4jErrorCodeAttribute : Attribute
{
    public string Code { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="Neo4jErrorCodeAttribute"/> class.
    /// </summary>
    /// <param name="code">The error code that the decorated class is the exception for.</param>
    public Neo4jErrorCodeAttribute(string code)
    {
        Code = code;
    }
}
