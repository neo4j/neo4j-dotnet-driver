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

using Neo4j.Driver.Bolt.PackStream.Ephemeral;

namespace Neo4j.Driver.Bolt.Messages.Ephemeral;

/// <summary>
/// Ephemeral view of a Bolt RECORD message. One field: list of record field values.
/// </summary>
public readonly struct RecordMessageView
{
    private readonly PackStreamStructView _structView;

    internal RecordMessageView(PackStreamStructView structView)
    {
        _structView = structView;
    }

    /// <summary>
    /// The list of field values (first and only field of the RECORD struct).
    /// </summary>
    public PackStreamListView Fields => _structView.Fields.ElementAt(0).ListValue;
}
