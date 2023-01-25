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

using Neo4j.Driver.Experimental.FluentQueries;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Methods being considered for moving to the Neo4j.Driver.
/// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
/// </summary>
public static class ExperimentalExtensions
{
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Sets the <see cref="IBookmarkManager"/> for maintaining bookmarks for the lifetime of a session.
    /// </summary>
    /// <param name="builder">this builder.</param>
    /// <param name="bookmarkManager">Instance of <see cref="IBookmarkManager"/>.</param>
    /// <returns>this <see cref="SessionConfigBuilder"/> instance.</returns>
    public static SessionConfigBuilder WithBookmarkManager(this SessionConfigBuilder builder, IBookmarkManager bookmarkManager)
    {
        return builder.WithBookmarkManager(bookmarkManager);
    }

    public static IExecutableQuery ExecutableQuery(this IDriver driver, string cypher)
    {
        return new ExecutableQuery((IInternalDriver)driver, cypher);
    }
}
