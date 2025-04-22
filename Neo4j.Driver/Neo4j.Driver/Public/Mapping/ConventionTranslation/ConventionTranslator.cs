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
/// Translates a string from one naming convention to another.
/// </summary>
/// <param name="objectIdentifierParser">The object identifier parser.</param>
/// <param name="recordFieldFormatter">The record field formatter.</param>
/// <typeparam name="T">The type of data that is parsed and formatted.</typeparam>
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
