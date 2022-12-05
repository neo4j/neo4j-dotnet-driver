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
/// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
/// <br/> This class provides access to experimental APIs on existing non-static classes.
/// </summary>
public static class ExperimentalExtensions
{
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// <br/> Sets the <see cref="IBookmarkManager"/> for maintaining bookmarks for the lifetime of the session.
    /// </summary>
    /// <param name="builder">This <see cref="SessionConfigBuilder"/> instance.</param>
    /// <param name="bookmarkManager">An instance of <see cref="IBookmarkManager"/> to use in the session.</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance.</returns>
    public static SessionConfigBuilder WithBookmarkManager(
        this SessionConfigBuilder builder,
        IBookmarkManager bookmarkManager)
    {
        return builder.WithBookmarkManager(bookmarkManager);
    }

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// <br/> Experimental: This method will be removed and replaced with a readonly property "BookmarkManager" on the
    /// <see cref="SessionConfig"/> class.<br/> Gets the configured experimental bookmark manager from this
    /// <see cref="SessionConfig"/> instance.
    /// </summary>
    /// <seealso cref="WithBookmarkManager"/>
    /// <param name="config">This <see cref="SessionConfig"/> instance.</param>
    /// <returns>This <see cref="SessionConfig"/>'s configured <see cref="IBookmarkManager"/> instance.</returns>
    public static IBookmarkManager GetBookmarkManager(this SessionConfig config)
    {
        return config.BookmarkManager;
    }
}
