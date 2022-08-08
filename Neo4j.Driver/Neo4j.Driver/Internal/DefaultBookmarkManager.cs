﻿// Copyright (c) 2002-2022 "Neo4j,"
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

namespace Neo4j.Driver.Internal;

internal class DefaultBookmarkManager : IBookmarkManager
{
    private readonly Dictionary<string, HashSet<string>> _bookmarkSets;
    private readonly Func<string, string[]> _bookmarkSupplier;
    private readonly Action<string, string[]> _onBookmarks;
    private readonly SemaphoreSlim _lock;

    public DefaultBookmarkManager(BookmarkManagerConfig config)
    {
        _bookmarkSets = config.InitialBookmarks.ToDictionary(x => x.Key, x => new HashSet<string>(x.Value));
        _bookmarkSupplier = config.BookmarkSupplier ?? (_ => Array.Empty<string>());
        _onBookmarks = config.NotifyBookmarks ?? ((_,_) => {});
        _lock = new SemaphoreSlim(1, 1);
    }

    public void UpdateBookmarks(string database, string[] previousBookmarks, string[] newBookmarks)
    {
        _lock.Wait();
        try
        {
            if (_bookmarkSets.TryGetValue(database, out var set))
            {
                previousBookmarks ??= Array.Empty<string>();
                foreach (var bookmarkToRemove in previousBookmarks)
                    set.Remove(bookmarkToRemove);

                foreach (var newBookmark in newBookmarks)
                    set.Add(newBookmark);
            }
            else
                _bookmarkSets.Add(database, new HashSet<string>(newBookmarks));
        }
        finally
        {
            _lock.Release();
        }

        _onBookmarks?.Invoke(database, newBookmarks);
    }

    public Bookmarks GetBookmarks(string database)
    {
        _lock.Wait();
        try
        {
            return Bookmarks.From(BookmarksFor(database));
        }
        finally
        {
            _lock.Release();
        }
    }

    private string[] BookmarksFor(string database)
    {
        if (!_bookmarkSets.TryGetValue(database, out var set))
            set = new HashSet<string>();

        if (_bookmarkSupplier == null)
            return set.ToArray();

        var supplied = _bookmarkSupplier(database);
        return set.Union(supplied).ToArray();
    }

    public string[] GetAllBookmarks(params string[] databases)
    {
        _lock.Wait();
        try
        {
            var keys = _bookmarkSets.Keys.Union(databases).ToArray();

            var set = new HashSet<string>();

            foreach (var key in keys)
            {
                var bookmarks = BookmarksFor(key);
                set.UnionWith(bookmarks);
            }

            return set.ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Forget(params string[] databases)
    {
        _lock.Wait();
        try
        {
            foreach (var database in databases)
                _bookmarkSets.Remove(database);
        }
        finally
        {
            _lock.Release();
        }
    }
}