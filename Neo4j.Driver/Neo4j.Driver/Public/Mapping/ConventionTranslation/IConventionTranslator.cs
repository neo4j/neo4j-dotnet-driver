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
/// Translates C# identifier names into database field names (and optionally vice versa) to support
/// automatic naming-convention mapping.
/// </summary>
/// <remarks>
/// <para>
/// The built-in implementation is <see cref="ConventionTranslator{T}"/>, which composes an
/// <see cref="IIdentifierParser{T}"/> and an <see cref="IFieldFormatter{T}"/>. For most scenarios, use
/// <see cref="RecordObjectMapping.TranslateIdentifiers(bool)"/> or one of its overloads, which create and
/// register a <see cref="ConventionTranslator{T}"/> configured from the
/// <see cref="IdentifierCaseConvention"/> and <see cref="FieldCaseConvention"/> enums.
/// </para>
/// <para>
/// Implement this interface directly when neither the built-in parser/formatter combinations nor the enums
/// cover your naming convention (for example, a custom prefix/suffix scheme).
/// Register a <see cref="ConventionTranslator{T}"/> at startup using
/// <see cref="RecordObjectMapping.TranslateIdentifiers{TParseResult}(IIdentifierParser{TParseResult},IFieldFormatter{TParseResult},bool)"/>,
/// or implement <see cref="IIdentifierParser{T}"/> and <see cref="IFieldFormatter{T}"/> and pass them to that overload.
/// </para>
/// </remarks>
public interface IConventionTranslator
{
    /// <summary>
    /// Translates a C# identifier name to the corresponding database field name.
    /// </summary>
    /// <param name="input">The C# identifier to translate.</param>
    /// <returns>The translated database field name.</returns>
    public string Translate(string input);
}
