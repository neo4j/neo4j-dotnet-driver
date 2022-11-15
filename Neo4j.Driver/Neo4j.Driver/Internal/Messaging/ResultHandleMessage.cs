// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Messaging;

internal abstract class ResultHandleMessage : IRequestMessage
{
    public const long NoQueryId = -1;
    public const long All = -1;

    protected ResultHandleMessage(long id, long n)
    {
        Metadata = id == NoQueryId
            ? new Dictionary<string, object> { { "n", n } }
            : new Dictionary<string, object> { { "n", n }, { "qid", id } };
    }

    protected abstract string Name { get; }

    public IDictionary<string, object> Metadata { get; }

    public abstract IPackStreamSerializer Serializer { get; }

    protected bool Equals(ResultHandleMessage other)
    {
        return Equals(Metadata, other.Metadata);
    }

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

        return Equals((ResultHandleMessage)obj);
    }

    public override int GetHashCode()
    {
        return Metadata != null ? Metadata.GetHashCode() : 0;
    }

    public override string ToString()
    {
        return $"{Name} {Metadata.ToContentString()}";
    }
}
