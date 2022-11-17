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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Experimental: Subject to change. Manager of Neo4j's causal-consistency mechanism, bookmarks.<br/> The manager
/// maintains and provides collections of bookmarks for databases, exposing to both driver and user-code.<br/>
/// </summary>
public interface IBookmarkManager
{
    /// <summary>Updates the bookmark manager's last known bookmarks for a database.</summary>
    /// <param name="database">Database which the bookmarks belong to.</param>
    /// <param name="previousBookmarks">The bookmarks used at the start of bookmark.</param>
    /// <param name="newBookmarks">The bookmarks to replace previousBookmarks with.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UpdateBookmarksAsync(
        string database,
        string[] previousBookmarks,
        string[] newBookmarks,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves last known bookmarks for a database.</summary>
    /// <param name="database">Database to get latest known bookmarks for.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Last known bookmarks for database.</returns>
    Task<string[]> GetBookmarksAsync(string database, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all bookmarks.</summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Last known bookmarks for all databases.</returns>
    Task<string[]> GetAllBookmarksAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes all or specified databases from the bookmark manager's internal cache.</summary>
    /// <param name="databases">databases to remove, or if empty: all.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ForgetAsync(string[] databases = null, CancellationToken cancellationToken = default);
}
