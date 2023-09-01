// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Preview.Mapping;

internal static class NodeAsRecordExtensions
{
    public static IRecord AsRecord(this INode node)
    {
        return new NodeAsRecord(node);
    }
}

internal class NodeAsRecord : IRecord
{
    private readonly INode _node;

    public NodeAsRecord(INode node)
    {
        _node = node;
    }

    /// <inheritdoc />
    public object this[int index] => throw new NotImplementedException("NodeAsRecord does not support index access");

    /// <inheritdoc />
    public object this[string key]
    {
        get
        {
            if (_node.Properties.ContainsKey(key))
            {
                return _node[key];
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Values => _node.Properties;

    /// <inheritdoc />
    public IReadOnlyList<string> Keys => _node.Properties.Keys.ToList();
}
