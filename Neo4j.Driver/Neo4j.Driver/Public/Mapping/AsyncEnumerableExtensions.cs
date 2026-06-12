// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains extension methods for mapping <see cref="IAsyncEnumerable{T}">IAsyncEnumerable&lt;IRecord&gt;</see>
/// streams to C# objects.
/// </summary>
/// <remarks>
/// <para>
/// Use these extensions when consuming records from a session or transaction cursor — that is, when you are
/// iterating the result of <c>RunAsync</c> or a transaction function. For the simpler
/// <see cref="IDriver.ExecutableQuery(string)"/> API, use
/// <see cref="ExecutableQueryMappingExtensions"/> instead.
/// </para>
/// <para>
/// Two materialisation styles are available:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="ToListAsync{T}"/> / <see cref="ToListFromBlueprintAsync{T}"/> — buffers all records into a
/// <c>IReadOnlyList&lt;T&gt;</c>.
/// </description></item>
/// <item><description>
/// <see cref="AsObjectsAsync{T}"/> / <see cref="AsObjectsFromBlueprintAsync{T}"/> — returns a lazy
/// <see cref="IAsyncEnumerable{T}"/> that maps each record on demand, without buffering the entire result set.
/// </description></item>
/// </list>
/// <para>
/// See
/// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
/// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
/// </para>
/// </remarks>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type <typeparamref name="T"/>, by
    /// mapping each record in the enumerable to an object. If no custom mapper is defined for type <typeparamref name="T"/>,
    /// the default mapper will be used.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="T">The type of object to map to.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static async ValueTask<IReadOnlyList<T>> ToListAsync<T>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            list.Add(item.AsObject<T>());
        }

        return list;
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type <typeparamref name="T"/>, by
    /// mapping each record in the enumerable to an object of the same type as <paramref name="blueprint"/>. This object could
    /// be anonymously typed. If no custom mapper is defined for type <typeparamref name="T"/>, the default mapper will be
    /// used.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="blueprint">An object of type <typeparamref name="T"/> to use as a blueprint for mapping.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="T">The type of object to map to.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static async ValueTask<IReadOnlyList<T>> ToListFromBlueprintAsync<T>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        T blueprint,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            list.Add(item.AsObjectFromBlueprint(blueprint));
        }

        return list;
    }

    /// <summary>
    /// Converts the <see cref="IAsyncEnumerable{IRecord}"/> to an <see cref="IAsyncEnumerable{T}"/> of objects of
    /// type <typeparamref name="T"/>, by mapping each record in the enumerable to an object. If no custom mapper is defined
    /// for type <typeparamref name="T"/>, the default mapper will be used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="T">The type of object to map to.</typeparam>
    /// <returns>An IAsyncEnumerable of the mapped objects.</returns>
    public static async IAsyncEnumerable<T> AsObjectsAsync<T>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            yield return item.AsObject<T>();
        }
    }

    /// <summary>
    /// Converts the <see cref="IAsyncEnumerable{IRecord}"/> to an <see cref="IAsyncEnumerable{T}"/> of objects of
    /// type <typeparamref name="T"/>, by mapping each record in the enumerable to an object of the same type as
    /// <paramref name="blueprint"/>. This object could be anonymously typed. If no custom mapper is defined for type
    /// <typeparamref name="T"/>, the default mapper will be used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="blueprint">An object of type <typeparamref name="T"/> to use as a blueprint for mapping.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="T">The type of object to map to.</typeparam>
    /// <returns>An IAsyncEnumerable of the mapped objects.</returns>
    public static async IAsyncEnumerable<T> AsObjectsFromBlueprintAsync<T>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        T blueprint,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            yield return item.AsObjectFromBlueprint(blueprint);
        }
    }
}
