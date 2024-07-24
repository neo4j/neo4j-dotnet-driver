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

namespace Neo4j.Driver.Internal.Result;

internal class InputPosition : IInputPosition
{
    public InputPosition(int offset, int line, int column)
    {
        Offset = offset;
        Line = line;
        Column = column;
    }

    public int Offset { get; }
    public int Line { get; }
    public int Column { get; }

    protected bool Equals(InputPosition other)
    {
        return Offset == other.Offset && Line == other.Line && Column == other.Column;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((InputPosition)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + Offset.GetHashCode();
            hash = hash * 23 + Line.GetHashCode();
            hash = hash * 23 + Column.GetHashCode();
            return hash;
        }
    }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Offset)}={Offset}, " +
            $"{nameof(Line)}={Line}, " +
            $"{nameof(Column)}={Column}}}";
    }

    internal static InputPosition ConvertFromDictionary(IDictionary<string, object> parent, string key)
    {
        if (!parent.TryGetValue(key, out var x) || x is not IDictionary<string, object> positionDictionary)
        {
            return null;
        }

        var offsetFound = positionDictionary.TryGetValue("offset", 0L, out var offset);
        var lineFound = positionDictionary.TryGetValue("line", 0L, out var line);
        var columnFound = positionDictionary.TryGetValue("column", 0L, out var column);
        if (offsetFound || lineFound || columnFound)
        {
            return new InputPosition((int)offset, (int)line, (int)column);
        }

        return null;
    }
}
