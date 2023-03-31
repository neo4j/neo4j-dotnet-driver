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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview;

namespace Neo4j.Driver.Internal;

internal class DefaultBookmarkManager : IBookmarkManager
{
    private readonly HashSet<string> _bookmarkSet;
    private readonly Func<CancellationToken, Task<string[]>> _bookmarkSupplier;
    private readonly SemaphoreSlim _lock;
    private readonly Func<string[], CancellationToken, Task> _onBookmarks;

    public DefaultBookmarkManager(BookmarkManagerConfig config)
    {
        _bookmarkSet = config.InitialBookmarks == null
            ? new HashSet<string>()
            : new HashSet<string>(config.InitialBookmarks);

        _bookmarkSupplier = config.BookmarkSupplierAsync;
        _onBookmarks = config.NotifyBookmarksAsync;
        _lock = new SemaphoreSlim(1, 1);
    }

    public async Task UpdateBookmarksAsync(
        string[] previousBookmarks,
        string[] newBookmarks,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            previousBookmarks ??= Array.Empty<string>();
            foreach (var bookmarkToRemove in previousBookmarks)
            {
                _bookmarkSet.Remove(bookmarkToRemove);
            }

            foreach (var newBookmark in newBookmarks)
            {
                _bookmarkSet.Add(newBookmark);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (_onBookmarks != null)
        {
            await _onBookmarks(newBookmarks, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string[]> GetBookmarksAsync(CancellationToken cancellationToken = default)
    {
        HashSet<string> set;
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            set = new HashSet<string>(_bookmarkSet);
        }
        finally
        {
            _lock.Release();
        }

        if (_bookmarkSupplier == null)
        {
            return set.ToArray();
        }

        set.UnionWith(await _bookmarkSupplier(cancellationToken).ConfigureAwait(false));

        return set.ToArray();
    }
}
