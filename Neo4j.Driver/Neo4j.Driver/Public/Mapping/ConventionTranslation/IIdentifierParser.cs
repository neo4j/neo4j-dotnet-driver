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
public interface IIdentifierParser<out T>
{
    /// <summary>
    /// Parse the input string into data that can then be used to create a string in the
    /// desired format.
    /// </summary>
    /// <param name="input">The string to parse.</param>
    /// <returns>The parsed data.</returns>
    public T ParseIdentifier(string input);
}
