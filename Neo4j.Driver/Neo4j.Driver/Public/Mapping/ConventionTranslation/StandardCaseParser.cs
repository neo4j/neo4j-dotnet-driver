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
using System.Linq;
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Mapping.ConventionTranslation;

/// <summary>
/// Parses a string according to a standard naming convention.
/// </summary>
public class StandardCaseParser : IIdentifierParser<IReadOnlyList<string>>
{
    private readonly string _validationRegex;
    private readonly string _splitRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardCaseParser"/> class.
    /// </summary>
    /// <param name="convention">The naming convention to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported convention is provided.</exception>
    public StandardCaseParser(IdentifierCaseConvention convention)
    {
        (_validationRegex, _splitRegex) = convention switch
        {
            IdentifierCaseConvention.CamelCase => ("^[a-z]+(?:[A-Z][a-z]*)*$", "(?<!^)(?=[A-Z][a-z])"),
            IdentifierCaseConvention.PascalCase => ("^[A-Z][a-z]*(?:[A-Z][a-z]*)*$", "(?<!^)(?=[A-Z])"),
            IdentifierCaseConvention.SnakeCase => ("^[a-z]+(?:_[a-z]+)*$", "_"),
            IdentifierCaseConvention.ScreamingSnakeCase => ("^[A-Z]+(?:_[A-Z]+)*$", "_"),
            IdentifierCaseConvention.KebabCase => ("^[a-z]+(?:-[a-z]+)*$", "-"),
            IdentifierCaseConvention.CSharpIdentifier => ("^[a-zA-Z]+(?:[A-Z][a-z]*)*$", "(?<!^)(?=[A-Z])"),
            _ => throw new System.ArgumentOutOfRangeException(nameof(convention), "Unsupported convention")
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ParseIdentifier(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new List<string>();
        }

        if (_validationRegex != null && !Regex.IsMatch(input, _validationRegex))
        {
            throw new System.ArgumentException($"Input '{input}' does not match the expected convention.");
        }

        return Regex.Split(input, _splitRegex)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
