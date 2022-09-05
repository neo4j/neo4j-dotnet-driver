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

namespace Neo4j.Driver;

/// <summary>
/// Configuration for constructing a default <see cref="IBookmarkManager"/> using <see cref="IBookmarkManagerFactory.NewBookmarkManager"/>.<br/>
/// the default <see cref="IBookmarkManagerFactory"/> can be accessed from <see cref="GraphDatabase.BookmarkManagerFactory"/>.
/// </summary>
/// <param name="InitialBookmarks">Nullable collection of initial bookmarks to provide the bookmark manager, the keys should be database names.</param>
/// <param name="BookmarkSupplier">Nullable delegate to provide externally sourced bookmarks to the driver.<br/>
/// Invoked when updating a cluster routing table, beginning transaction, or running a query from a session.<br/>
/// the argument will be either a database name when driver calls <see cref="IBookmarkManager.GetBookmarks"/> or null when the driver calls <see cref="IBookmarkManager.GetAllBookmarks"/>.</param>
/// <param name="NotifyBookmarks">Nullable delegate to notify application of new bookmarks received by the driver from the server for a database.</param>
public record BookmarkManagerConfig(
    Dictionary<string, IEnumerable<string>> InitialBookmarks,
    Func<string, string[]> BookmarkSupplier,
    Action<string, string[]> NotifyBookmarks);