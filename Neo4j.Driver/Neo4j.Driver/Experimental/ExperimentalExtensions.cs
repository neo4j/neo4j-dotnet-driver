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

using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Methods being considered for moving to the Neo4j.Driver.GraphDatabase.
/// </summary>
public static class ExperimentalExtensions
{
    /// <summary>
    /// Experimental: Bookmark Manager API is still under consideration. <br/>
    /// Gets a new <see cref="IBookmarkManagerFactory"/>, which can construct a default implementation of <see cref="IBookmarkManager"/>.<br/>
    /// <see cref="IBookmarkManager"/> instances can be passed to <see cref="SessionConfigBuilder"/> when opening a new session.
    /// </summary>
    public static IBookmarkManagerFactory BookmarkManagerFactory => new BookmarkManagerFactory();
}