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

namespace Neo4j.Driver;

/// <summary>An input position refers to a specific character in a query.</summary>
public interface IInputPosition
{
    /// <summary>Gets the character offset referred to by this position; offset numbers start at 0.</summary>
    int Offset { get; }

    /// <summary>Gets the line number referred to by the position; line numbers start at 1.</summary>
    int Line { get; }

    /// <summary>Gets the column number referred to by the position; column numbers start at 1.</summary>
    int Column { get; }
}
