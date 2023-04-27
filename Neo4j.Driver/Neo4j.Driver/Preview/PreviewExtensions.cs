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

using System.Collections.Generic;
using Neo4j.Driver.FluentQueries;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Preview;

/// <summary>
/// There is no guarantee that anything in Neo4j.Driver.Preview namespace will be in a next minor version.
/// <br/> This class provides access to preview APIs on existing non-static classes.
/// </summary>
public static class PreviewExtensions
{
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Preview namespace will be in a next minor version.
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
    /// There is no guarantee that anything in Neo4j.Driver.Preview namespace will be in a next minor version.
    /// Gets an <see cref="IExecutableQuery&lt;IRecord&gt;"/> that can be used to configure and execute a query using fluent
    /// method chaining.
    /// </summary>
    /// <example>
    /// The following example configures and executes a simple query, then iterates over the results.
    /// <code language="cs">
    ///  var eagerResult = await driver
    ///      .ExecutableQueryBuilder("MATCH (m:Movie) WHERE m.released > $releaseYear RETURN m.title AS title")
    ///      .WithParameters(new { releaseYear = 2005 })
    ///      .ExecuteAsync();
    ///  <para></para>
    ///  foreach(var record in eagerResult.Result)
    ///  {
    ///      Console.WriteLine(record["title"].As&lt;string&gt;());
    ///  }
    ///  </code>
    /// <para></para>
    /// The following example gets a single scalar value from a query.
    /// <code>
    ///  var born = await driver
    ///      .ExecutableQueryBuilder("MATCH (p:Person WHERE p.name = $name) RETURN p.born AS born")
    ///      .WithStreamProcessor(async stream => (await stream.Where(_ => true).FirstAsync())["born"].As&lt;int&gt;())
    ///      .WithParameters(new Dictionary&lt;string, object&gt; { ["name"] = "Tom Hanks" })
    ///      .ExecuteAsync();
    ///  <para></para>
    ///  Console.WriteLine($"Tom Hanks born {born.Result}");
    ///  </code>
    /// </example>
    /// <param name="driver">The driver.</param>
    /// <param name="cypher">The cypher of the query.</param>
    /// <returns>
    /// An <see cref="IExecutableQuery&lt;IRecord&gt;"/> that can be used to configure and execute a query using
    /// fluent method chaining.
    /// </returns>
    public static IExecutableQuery<IRecord, IRecord> GetExecutableQuery(this IDriver driver, string cypher)
    {
        return new ExecutableQuery<IRecord, IRecord>(
            new DriverRowSource((IInternalDriver)driver, cypher),
            x => x);
    }

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Preview namespace will be in a next minor version.
    /// <br/> Preview: This method will be removed and replaced with a readonly property "BookmarkManager" on the
    /// <see cref="SessionConfig"/> class.<br/> Gets the configured preview bookmark manager from this
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
