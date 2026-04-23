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
/// Marks the constructor that the default mapper should use when creating an instance of the decorated type.
/// </summary>
/// <remarks>
/// <para>
/// By default, the mapper selects the constructor with the fewest parameters. Apply this attribute to a
/// different constructor when you want to use one with more parameters, or when your type has multiple
/// constructors and the selection would otherwise be ambiguous.
/// </para>
/// <para>
/// For C# <c>record</c> types the primary constructor is used automatically; this attribute is typically
/// not needed for records.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Constructor)]
public class MappingConstructorAttribute : Attribute
{
}
