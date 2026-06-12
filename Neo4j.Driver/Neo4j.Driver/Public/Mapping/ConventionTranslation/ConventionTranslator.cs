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
/// Translates C# identifier names to database field names by combining a parser and a formatter.
/// </summary>
/// <remarks>
/// <para>
/// This is the standard implementation of <see cref="IConventionTranslator"/>. It first calls
/// <see cref="IIdentifierParser{T}.ParseIdentifier"/> on the input to break it into tokens, then passes those
/// tokens to <see cref="IFieldFormatter{T}.Format"/> to produce the output field name.
/// </para>
/// <para>
/// Use <see cref="StandardCaseParser"/> and <see cref="StandardCaseFormatter"/> for the built-in naming
/// conventions, or supply custom implementations for advanced scenarios. Pass the resulting translator to
/// <see cref="RecordObjectMapping.TranslateIdentifiers{TParseResult}(IIdentifierParser{TParseResult},IFieldFormatter{TParseResult},bool)"/>.
/// </para>
/// </remarks>
/// <param name="objectIdentifierParser">Parses C# identifiers into an intermediate representation.</param>
/// <param name="recordFieldFormatter">Formats the intermediate representation into a database field name.</param>
/// <typeparam name="T">The type of the intermediate representation shared between parser and formatter.</typeparam>
public class ConventionTranslator<T>(IIdentifierParser<T> objectIdentifierParser, IFieldFormatter<T> recordFieldFormatter)
    : IConventionTranslator
{
    /// <inheritdoc />
    public string Translate(string input)
    {
        var extractedTokens = objectIdentifierParser.ParseIdentifier(input);
        var recombinedText = recordFieldFormatter.Format(extractedTokens);
        return recombinedText;
    }
}
