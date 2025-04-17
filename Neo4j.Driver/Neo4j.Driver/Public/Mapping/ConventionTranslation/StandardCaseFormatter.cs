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
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Helpers;

namespace Neo4j.Driver.Mapping.ConventionTranslation;

/// <summary>
/// Formats a list of tokens into a string using a specified standard case convention.
/// </summary>
public class StandardCaseFormatter : IFieldFormatter<IEnumerable<string>>
{
    private readonly FieldCaseConvention _convention;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardCaseFormatter"/> class with the specified case convention.
    /// </summary>
    /// <param name="convention">The case convention to use for formatting.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported case convention is provided.</exception>
    public StandardCaseFormatter(FieldCaseConvention convention)
    {
        if (!Enum.IsDefined(typeof(FieldCaseConvention), convention))
        {
            throw new ArgumentOutOfRangeException(nameof(convention), "Unsupported field case convention");
        }

        _convention = convention;
    }

    /// <inheritdoc />
    public string Format(IEnumerable<string> data)
    {
        var tokens = data.ToList();
        return _convention switch
        {
            FieldCaseConvention.SnakeCase => string.Join("_", tokens.Select(t => t.ToLower())),
            FieldCaseConvention.CamelCase => FormatCamelCase(tokens),
            FieldCaseConvention.PascalCase => FormatPascalCase(tokens),
            FieldCaseConvention.ScreamingSnakeCase => string.Join("_", tokens.Select(t => t.ToUpper())),
            FieldCaseConvention.KebabCase => string.Join("-", tokens.Select(t => t.ToLower())),
            _ => throw new InvalidOperationException("This code path should never be reached")
        };
    }

    private static string FormatPascalCase(IEnumerable<string> tokens)
    {
        return string.Concat(
            tokens.Select(t => char.ToUpper(t[0]) + t.Substring(1).ToLower()));
    }

    private static string FormatCamelCase(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        var firstToken = tokens[0].ToLower();
        var restTokens = tokens.Skip(1).Select(t => char.ToUpper(t[0]) + t.Substring(1).ToLower());
        return firstToken + string.Concat(restTokens);
    }
}
