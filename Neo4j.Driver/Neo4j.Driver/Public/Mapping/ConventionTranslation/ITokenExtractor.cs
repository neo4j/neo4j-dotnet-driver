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
/// Extracts tokens from a string. A token is a part of a string that is separated by either a
/// delimiter or a change in case.
/// </summary>
public interface ITokenExtractor
{
    public IReadOnlyList<string> ExtractTokens(string input);
}

/// <summary>
/// Combines tokens into a string according to the rules of the implementation.
/// </summary>
public interface ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens);
}

/// <summary>
/// Translates a string from one naming convention to another.
/// </summary>
public interface IConventionTranslator
{
    public string Translate(string input);
}
