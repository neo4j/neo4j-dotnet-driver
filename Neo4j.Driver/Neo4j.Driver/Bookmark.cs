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
using Neo4j.Driver.Internal;

namespace Neo4j.Driver;

/// <summary>
/// Identifies a point in the transactional history of the database.<br/><br/> When working with a casual cluster,
/// transactions can be chained to ensure causal consistency. Causal chaining is carried out by passing bookmarks between
/// transactions.<br/> When a session is constructed with an initial bookmarks, the first transaction (either auto-commit
/// or explicit) will be blocked until the server has fast forwarded to catchup with the latest of the provided initial
/// bookmarks.<br/> Within a session, bookmark propagation is carried out automatically and does not require any explicit
/// signal or setting from the application.<br/> To opt out of this mechanism for unrelated units of work, applications can
/// use multiple sessions.
/// </summary>
[Obsolete("Replaced with Bookmarks. Will be removed in 6.0")]
public abstract class Bookmark
{
    internal static readonly Bookmark Empty = new InternalBookmarks();

    /// <summary>Returns a list of bookmark strings that this bookmark instance identifies.</summary>
    public string[] Values { get; protected set; }

    /// <summary>Returns a new bookmark instance constructed from the provided list of bookmark strings.</summary>
    /// <param name="values">The bookmark strings to construct from</param>
    /// <returns>A new bookmark instance</returns>
    public static Bookmark From(params string[] values)
    {
        return new InternalBookmarks(values);
    }

    internal static Bookmark From(IEnumerable<Bookmark> bookmarks)
    {
        if (bookmarks == null)
        {
            throw new ArgumentNullException(nameof(bookmarks));
        }

        return new InternalBookmarks(
            bookmarks.SelectMany(b => b == null ? Array.Empty<string>() : b.Values)
                .Distinct()
                .ToArray());
    }

    public static Bookmark operator +(Bookmark lh, Bookmark rh)
    {
        return new InternalBookmarks(lh.Values.Concat(rh.Values).ToArray());
    }
}

/// <summary>
/// Identifies a point in the transactional history of the database.<br/><br/> When working with a casual cluster,
/// transactions can be chained to ensure causal consistency. Causal chaining is carried out by passing bookmarks between
/// transactions.<br/> When a session is constructed with an initial bookmarks, the first transaction (either auto-commit
/// or explicit) will be blocked until the server has fast forwarded to catchup with the latest of the provided initial
/// bookmarks.<br/> Within a session, bookmarks propagation is carried out automatically and does not require any explicit
/// signal or setting from the application.<br/> To opt out of this mechanism for unrelated units of work, applications can
/// use multiple sessions.
/// </summary>
public abstract class Bookmarks : Bookmark
{
    internal new static Bookmarks Empty => new InternalBookmarks();

    /// <summary></summary>
    /// <param name="lh"></param>
    /// <param name="rh"></param>
    /// <returns></returns>
    public static Bookmarks operator +(Bookmarks lh, Bookmarks rh)
    {
        return new InternalBookmarks(lh.Values.Concat(rh.Values).ToArray());
    }

    /// <summary>Returns a new bookmark instance constructed from the provided list of bookmark strings.</summary>
    /// <param name="values">The bookmark strings to construct from</param>
    /// <returns>A new bookmark instance</returns>
    public static Bookmarks From(params string[] values)
    {
        return new InternalBookmarks(values);
    }

    /// <summary>Returns a new bookmark instance constructed from the provided list of bookmark strings.</summary>
    /// <param name="values">The bookmark strings to construct from</param>
    /// <returns>A new bookmark instance</returns>
    public static Bookmarks From(IEnumerable<string> values)
    {
        return new InternalBookmarks(values);
    }

    internal static Bookmarks From(IEnumerable<Bookmarks> bookmarks)
    {
        if (bookmarks == null)
        {
            throw new ArgumentNullException(nameof(bookmarks));
        }

        var uniqueValues = bookmarks
            .SelectMany(b => b == null ? Array.Empty<string>() : b.Values)
            .Distinct()
            .ToArray();

        return new InternalBookmarks(uniqueValues);
    }
}
