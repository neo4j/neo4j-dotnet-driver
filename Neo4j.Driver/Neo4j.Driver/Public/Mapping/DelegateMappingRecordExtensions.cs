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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains extensions for mapping records to objects using delegates.
/// </summary>
public static class DelegateMappingRecordExtensions
{
    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with key <c>a</c>, the following code will map the record to an object with property
    /// <c>a</c> of type int:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a) => new { a });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1>(this IRecord record, Func<T1, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c> and <c>b</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int and <c>b</c> of type string:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a, string b) => new { a, b });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2>(
        this IRecord record,
        Func<T1, T2, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c> and <c>c</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string and <c>c</c> of type bool:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a, string b, bool c) => new { a, b, c });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3>(this IRecord record, Func<T1, T2, T3, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c> and <c>d</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool and <c>d</c> of type double:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a, string b, bool c, double d) => new { a, b, c, d });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4>(
        this IRecord record,
        Func<T1, T2, T3, T4, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c> and <c>e</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double and <c>e</c> of type long:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a, string b, bool c, double d, long e) => new { a, b, c, d, e });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c> and <c>f</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double, <c>e</c> of type long and <c>f</c> of type float:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject((int a, string b, bool c, double d, long e, float f) => new { a, b, c, d, e, f });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5, T6>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, T6, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c> and <c>g</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double, <c>e</c> of type long, <c>f</c> of type float and <c>g</c> of type decimal:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject(
    ///     (int a, string b, bool c, double d, long e, float f, decimal g) =>
    ///         new { a, b, c, d, e, f, g });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5, T6, T7>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c> and <c>h</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double, <c>e</c> of type long, <c>f</c> of type float, <c>g</c> of type decimal and <c>h</c> of type byte:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject(
    ///     (int a, string b, bool c, double d, long e, float f, decimal g, byte h) =>
    ///         new { a, b, c, d, e, f, g, h });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c>, <c>h</c> and <c>i</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double, <c>e</c> of type long, <c>f</c> of type float, <c>g</c> of type decimal, <c>h</c> of type byte and <c>i</c> of type short:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject(
    ///     (int a, string b, bool c, double d, long e, float f, decimal g, byte h, short i) =>
    ///         new { a, b, c, d, e, f, g, h, i });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the delegate.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }

    /// <summary>
    /// Converts the record to an object using the given delegate.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A delegate that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the delegate will be used to lookup values in the record, those values
    /// will be converted to the types of the parameters, and then the delegate will be invoked to create the result
    /// object.</param>
    /// <example>
    /// Given a record with keys <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c>, <c>h</c>, <c>i</c> and <c>j</c>, the following code will map the record to an object with properties
    /// <c>a</c> of type int, <c>b</c> of type string, <c>c</c> of type bool, <c>d</c> of type double, <c>e</c> of type long, <c>f</c> of type float, <c>g</c> of type decimal, <c>h</c> of type byte, <c>i</c> of type short and <c>j</c> of type ushort:
    /// <code language="c#">
    /// var record = ...; // obtain a record somehow
    /// var result = record.AsObject(
    ///     (int a, string b, bool c, double d, long e, float f, decimal g, byte h, short i, ushort j) =>
    ///         new { a, b, c, d, e, f, g, h, i, j });
    /// </code>
    /// </example>
    /// <typeparam name="TResult">The type that the supplied delegate returns.</typeparam>
    /// <typeparam name="T1">The type of the first parameter of the delegate.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the delegate.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the delegate.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the delegate.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the delegate.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the delegate.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the delegate.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the delegate.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter of the delegate.</typeparam>
    /// <typeparam name="T10">The type of the tenth parameter of the delegate.</typeparam>
    /// <returns>The mapped object.</returns>
    public static TResult AsObject<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this IRecord record,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> map)
    {
        return DelegateMapper.MapWithMethodInfo<TResult>(record, map.Method, map.Target);
    }
}
