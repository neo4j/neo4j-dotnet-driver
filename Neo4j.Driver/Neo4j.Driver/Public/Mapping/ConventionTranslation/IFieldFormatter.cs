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

namespace Neo4j.Driver.Mapping.ConventionTranslation;

/// <summary>
/// Formats an intermediate representation produced by an <see cref="IIdentifierParser{T}"/> into a database
/// field name string.
/// </summary>
/// <remarks>
/// The built-in implementation is <see cref="StandardCaseFormatter"/>, which handles the conventions defined by
/// <see cref="FieldCaseConvention"/>. Implement this interface to produce field names in a convention not
/// covered by that enum.
/// </remarks>
/// <typeparam name="T">The type of the intermediate representation consumed by this formatter.</typeparam>
public interface IFieldFormatter<in T>
{
    /// <summary>
    /// Formats the parsed identifier data into a database field name.
    /// </summary>
    /// <param name="data">The parsed representation produced by a matching <see cref="IIdentifierParser{T}"/>.</param>
    /// <returns>The formatted database field name.</returns>
    public string Format(T data);
}
