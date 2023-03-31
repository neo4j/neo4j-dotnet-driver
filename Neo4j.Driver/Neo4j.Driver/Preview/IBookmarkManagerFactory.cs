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

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Experimental: Subject to change.<br/> The <see cref="IBookmarkManagerFactory"/> interface is intended for
/// classes that construct instances of an <see cref="IBookmarkManager"/> implementation.
/// </summary>
public interface IBookmarkManagerFactory
{
    /// <summary>Create an <see cref="IBookmarkManager"/> instance with specified configuration.</summary>
    /// <param name="config">The configuration object for constructing a new <see cref="IBookmarkManager"/>.</param>
    /// <returns>
    /// a new <see cref="IBookmarkManager"/> instance instantiated with the <see cref="BookmarkManagerConfig"/>
    /// parameter.
    /// </returns>
    IBookmarkManager NewBookmarkManager(BookmarkManagerConfig config = null);
}
