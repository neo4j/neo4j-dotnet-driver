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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// The <see cref="IBookmarkManager"/> interface is intended for implementation by classes that provide convenient
/// interfacing with <see cref="Bookmarks"/> in both the driver and user code.
/// </summary>
/// <seealso cref="SessionConfigBuilder.WithBookmarkManager"/>
public interface IBookmarkManager
{
    /// <summary>
    /// Update the <see cref="IBookmarkManager"/>, removing values in <paramref name="previousBookmarks"/> and
    /// inserting values in <paramref name="newBookmarks"/>.
    /// </summary>
    /// <param name="previousBookmarks">
    /// The bookmarks used at the beginning of the causally chained process such as
    /// <see cref="IAsyncTransaction.CommitAsync"/>.
    /// </param>
    /// <param name="newBookmarks">
    /// The Bookmarks received from completing a causally chained process such as
    /// <see cref="IAsyncTransaction.CommitAsync"/>.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes when the <see cref="IBookmarkManager"/> has been updated.</returns>
    Task UpdateBookmarksAsync(
        string[] previousBookmarks,
        string[] newBookmarks,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieve all bookmarks from the <see cref="IBookmarkManager"/>.</summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes when the <see cref="IBookmarkManager"/> has collected bookmarks.</returns>
    Task<string[]> GetBookmarksAsync(CancellationToken cancellationToken = default);
}
