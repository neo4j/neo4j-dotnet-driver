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

using System.Collections.Generic;

namespace Neo4j.Driver.Mapping.ConventionTranslation;

/// <summary>
/// Parses a C# identifier into an intermediate representation that can then be formatted by an
/// <see cref="IFieldFormatter{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The built-in implementation is <see cref="StandardCaseParser"/>, which handles the naming conventions
/// defined by <see cref="IdentifierCaseConvention"/>. Implement this interface when your C# identifiers
/// follow a convention not covered by that enum.
/// </para>
/// <para>
/// Pair a custom parser with an <see cref="IFieldFormatter{T}"/> and pass both to
/// <see cref="RecordObjectMapping.TranslateIdentifiers{TParseResult}(IIdentifierParser{TParseResult},IFieldFormatter{TParseResult},bool)"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// The type of the intermediate representation produced by the parser and consumed by the formatter.
/// </typeparam>
public interface IIdentifierParser<out T>
{
    /// <summary>
    /// Parses the input identifier into an intermediate representation.
    /// </summary>
    /// <param name="input">The C# identifier to parse.</param>
    /// <returns>The parsed representation, to be passed to a matching <see cref="IFieldFormatter{T}"/>.</returns>
    public T ParseIdentifier(string input);
}
