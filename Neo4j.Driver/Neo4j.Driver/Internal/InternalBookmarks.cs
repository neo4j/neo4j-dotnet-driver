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

namespace Neo4j.Driver.Internal;

internal class InternalBookmarks : Bookmarks
{
    internal InternalBookmarks(params string[] values)
    {
        Values = values.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToArray();
    }

    internal InternalBookmarks(IEnumerable<string> values)
    {
        Values = values.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToArray();
    }

    private bool Equals(InternalBookmarks other)
    {
        if (Values.Length != other.Values.Length)
        {
            return false;
        }

        return Values.Except(other.Values).Any() == false;
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

        return Equals((InternalBookmarks)obj);
    }

    public override int GetHashCode()
    {
        return Values != null ? Values.GetHashCode() : 0;
    }
}
