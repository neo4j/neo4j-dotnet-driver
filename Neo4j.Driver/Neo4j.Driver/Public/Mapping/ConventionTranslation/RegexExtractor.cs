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

public class RegexExtractor(string validationRegex, string splitRegex) : ITokenExtractor
{
    public IReadOnlyList<string> ExtractTokens(string input)
    {
        if (validationRegex != null && !Regex.IsMatch(input, validationRegex))
        {
            throw new System.ArgumentException($"Input '{input}' does not match the expected convention.");
        }

        return Regex.Split(input, splitRegex)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}

public class CamelCaseExtractor() : RegexExtractor(@"^[a-z]+(?:[A-Z][a-z]*)*$", @"(?<!^)(?=[A-Z][a-z])");

public class PascalCaseExtractor() : RegexExtractor(@"^[A-Z][a-z]*(?:[A-Z][a-z]*)*$", @"(?<!^)(?=[A-Z])");

public class SnakeCaseExtractor() : RegexExtractor(@"^[a-z]+(?:_[a-z]+)*$", @"_");

public class ScreamingSnakeCaseExtractor() : RegexExtractor(@"^[A-Z]+(?:_[A-Z]+)*$", @"_");

public class KebabCaseExtractor() : RegexExtractor(@"^[a-z]+(?:-[a-z]+)*$", @"-");

public class CSharpIdentifierExtractor() : RegexExtractor(@"^[a-zA-Z]+(?:[A-Z][a-z]*)*$", @"(?<!^)(?=[A-Z])");
