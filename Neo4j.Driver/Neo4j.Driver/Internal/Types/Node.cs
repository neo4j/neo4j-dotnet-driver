﻿// Copyright (c) "Neo4j"
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

#pragma warning disable CS0618

namespace Neo4j.Driver.Internal.Types;

internal sealed class Node : INode
{
    public Node(long id, IReadOnlyList<string> labels, IReadOnlyDictionary<string, object> prop)
    {
        Id = id;
        ElementId = id.ToString();
        Labels = labels;
        Properties = prop;
    }

    public Node(long id, string elementId, IReadOnlyList<string> labels, IReadOnlyDictionary<string, object> prop)
    {
        Id = id;
        ElementId = elementId;
        Labels = labels;
        Properties = prop;
    }

    [Obsolete("Replaced with ElementId, Will be removed in 6.0")]
    public long Id { get; set; }

    public string ElementId { get; }
    public IReadOnlyList<string> Labels { get; }

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        return Properties[key].As<T>();
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T value)
    {
        if (Properties.TryGetValue(key, out var obj))
        {
            value = obj.As<T>();
            return true;
        }

        value = default;
        return false;
    }

    public IReadOnlyDictionary<string, object> Properties { get; }
    public object this[string key] => Properties[key];

    public bool Equals(INode other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(ElementId, other.ElementId);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as INode);
    }

    public override int GetHashCode()
    {
        return ElementId.GetHashCode();
    }
}
