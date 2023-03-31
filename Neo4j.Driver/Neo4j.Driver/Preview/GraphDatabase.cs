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

using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Preview;

/// <summary>
/// Methods being considered for moving to the Neo4j.Driver. There is no guarantee that anything in
/// Neo4j.Driver.Experimental namespace will be in a next minor version.
/// </summary>
public static class GraphDatabase
{
    /// <summary>
    /// Experimental: Bookmark Manager API is still under consideration. <br/> There is no guarantee that anything in
    /// Neo4j.Driver.Experimental namespace will be in a next minor version.<br/> Gets a new
    /// <see cref="IBookmarkManagerFactory"/>, which can construct a new default <see cref="IBookmarkManager"/> instance.<br/>
    /// The <see cref="IBookmarkManager"/> instance should be passed to <see cref="SessionConfigBuilder"/> when opening a new
    /// session with <see cref="ExperimentalExtensions.WithBookmarkManager"/>.
    /// </summary>
    public static IBookmarkManagerFactory BookmarkManagerFactory => new BookmarkManagerFactory();
}
