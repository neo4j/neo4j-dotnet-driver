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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Provides extension methods for materializing <see cref="IAsyncEnumerable{T}"/> into lists of objects
/// by mapping each record in the enumerable to an object using provided delegates.
/// </summary>
public static class DelegateAsyncEnumerableExtensions
{
    private static async ValueTask<IReadOnlyList<TResult>> ToListAsyncImpl<TResult>(
        IAsyncEnumerable<IRecord> asyncEnumerable,
        MethodInfo mapMethod,
        object target,
        CancellationToken cancellationToken)
    {
        var list = new List<TResult>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            list.Add(DelegateMapper.MapWithMethodInfo<TResult>(item, mapMethod, target));
        }

        return list;
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5, T6>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, T6, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the map delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the map delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the map delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the map delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the map delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the map delegate.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }

    /// <summary>
    /// Materializes the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="TResult"/>, by mapping each record in the enumerable to an object using
    /// the provided <paramref name="map"/> delegate.
    /// </summary>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <param name="map">The delegate to map each record to an object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <typeparam name="TResult">The type of object to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the map delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the map delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the map delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the map delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the map delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the map delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the map delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the map delegate.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter of the map delegate.</typeparam>
    /// <typeparam name="T10">The type of the tenth parameter of the map delegate.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this IAsyncEnumerable<IRecord> asyncEnumerable,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> map,
        CancellationToken cancellationToken = default)
    {
        return ToListAsyncImpl<TResult>(asyncEnumerable, map.Method, map.Target, cancellationToken);
    }
}
