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

namespace Neo4j.Driver.Internal.Types;

internal class Segment : ISegment
{
    public Segment(INode start, IRelationship rel, INode end)
    {
        Start = start;
        Relationship = rel;
        End = end;
    }

    public INode Start { get; }
    public INode End { get; }
    public IRelationship Relationship { get; }

    public bool Equals(ISegment other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(Start, other.Start) && Equals(End, other.End) && Equals(Relationship, other.Relationship);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ISegment);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Start?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (End?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Relationship?.GetHashCode() ?? 0);
            return hashCode;
        }
    }
}
