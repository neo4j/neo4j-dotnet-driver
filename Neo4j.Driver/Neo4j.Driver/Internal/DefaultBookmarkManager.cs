// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal class DefaultBookmarkManager : IBookmarkManager
{
    private readonly Func<string, CancellationToken, Task<string[]>> _bookmarkSupplier;
    private readonly SemaphoreSlim _lock;
    private readonly Func<string, string[], CancellationToken, Task> _onBookmarks;
    private Dictionary<string, HashSet<string>> _bookmarkSets;

    public DefaultBookmarkManager(BookmarkManagerConfig config)
    {
        _bookmarkSets = config.InitialBookmarks?.ToDictionary(x => x.Key, x => new HashSet<string>(x.Value)) ??
            new Dictionary<string, HashSet<string>>();

        _bookmarkSupplier = config.BookmarkSupplierAsync;
        _onBookmarks = config.NotifyBookmarksAsync;
        _lock = new SemaphoreSlim(1, 1);
    }

    public async Task UpdateBookmarksAsync(
        string database,
        string[] previousBookmarks,
        string[] newBookmarks,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bookmarkSets.TryGetValue(database, out var set))
            {
                previousBookmarks ??= Array.Empty<string>();
                foreach (var bookmarkToRemove in previousBookmarks)
                {
                    set.Remove(bookmarkToRemove);
                }

                foreach (var newBookmark in newBookmarks)
                {
                    set.Add(newBookmark);
                }
            }
            else
            {
                _bookmarkSets.Add(database, new HashSet<string>(newBookmarks));
            }
        }
        finally
        {
            _lock.Release();
        }

        if (_onBookmarks != null)
        {
            await _onBookmarks.Invoke(database, newBookmarks, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string[]> GetBookmarksAsync(string database, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> set;

        try
        {
            set = BookmarksFor(database);

            if (_bookmarkSupplier == null)
            {
                return set.ToArray();
            }
        }
        finally
        {
            _lock.Release();
        }

        var supplied = await _bookmarkSupplier(database, cancellationToken).ConfigureAwait(false);

        return set.Union(supplied).ToArray();
    }

    public async Task<string[]> GetAllBookmarksAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        var set = new HashSet<string>();

        try
        {
            var keys = _bookmarkSets.Keys.ToArray();

            foreach (var key in keys)
            {
                set.UnionWith(BookmarksFor(key));
            }
        }
        finally
        {
            _lock.Release();
        }

        if (_bookmarkSupplier == null)
        {
            return set.ToArray();
        }

        set.UnionWith(await _bookmarkSupplier(null, cancellationToken).ConfigureAwait(false));

        return set.ToArray();
    }

    public async Task ForgetAsync(string[] databases = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (databases != null && databases.Any())
            {
                foreach (var database in databases)
                {
                    _bookmarkSets.Remove(database);
                }
            }
            else
            {
                _bookmarkSets = new Dictionary<string, HashSet<string>>();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private HashSet<string> BookmarksFor(string database)
    {
        return _bookmarkSets.TryGetValue(database, out var dbBookmarks)
            ? dbBookmarks
            : new HashSet<string>();
    }
}
