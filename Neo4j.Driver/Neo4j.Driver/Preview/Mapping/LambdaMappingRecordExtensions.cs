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
using System.Linq.Expressions;

namespace Neo4j.Driver.Preview.Mapping;

/// <summary>
/// Contains extensions for mapping records to objects using lambda expressions.
/// </summary>
public static class LambdaMappingRecordExtensions
{
    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with key <c>a</c>, the following code will map the record to an object with property
    /// <c>A</c>, casting it to an int:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject(a => new
    /// {
    ///     A = a.As&lt;int&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record, Expression<Func<object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c> and <c>b</c>, the following code will map the record to an object with
    /// properties <c>A</c> and <c>B</c>, casting them to an int and a string respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record, Expression<Func<object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c> and <c>c</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c> and <c>C</c>, casting them to an int, a string and a bool respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record, Expression<Func<object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c> and <c>d</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c> and <c>D</c>, casting them to an int, a string, a bool and a double respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c> and <c>e</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c> and <c>E</c>, casting them to an int, a string, a bool, a double and a DateTime respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c> and <c>f</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c>, <c>E</c> and <c>F</c>, casting them to an int, a string, a bool, a double, a DateTime and a long respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e, f) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;(),
    ///     F = f.As&lt;long&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c> and <c>g</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c>, <c>E</c>, <c>F</c> and <c>G</c>, casting them to an int, a string, a bool, a double, a DateTime, a long and a float respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e, f, g) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;(),
    ///     F = f.As&lt;long&gt;(),
    ///     G = g.As&lt;float&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c> and <c>h</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c>, <c>E</c>, <c>F</c>, <c>G</c> and <c>H</c>, casting them to an int, a string, a bool, a double, a DateTime, a long, a float and a decimal respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e, f, g, h) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;(),
    ///     F = f.As&lt;long&gt;(),
    ///     G = g.As&lt;float&gt;(),
    ///     H = h.As&lt;decimal&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c>, <c>h</c> and <c>i</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c>, <c>E</c>, <c>F</c>, <c>G</c>, <c>H</c> and <c>I</c>, casting them to an int, a string, a bool, a double, a DateTime, a long, a float, a decimal and a char respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e, f, g, h, i) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;(),
    ///     F = f.As&lt;long&gt;(),
    ///     G = g.As&lt;float&gt;(),
    ///     H = h.As&lt;decimal&gt;(),
    ///     I = i.As&lt;char&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and then those values
    /// will be passed to the lambda function. </param>
    /// <example>
    /// Given a record with fields <c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>, <c>e</c>, <c>f</c>, <c>g</c>, <c>h</c>, <c>i</c> and <c>j</c>, the following code will map the record to an object with
    /// properties <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c>, <c>E</c>, <c>F</c>, <c>G</c>, <c>H</c>, <c>I</c> and <c>J</c>, casting them to an int, a string, a bool, a double, a DateTime, a long, a float, a decimal, a char and a byte respectively:
    /// <code language="c#">
    /// var record = ...;
    /// var result = record.AsObject( (a, b, c, d, e, f, g, h, i, j) => new
    /// {
    ///     A = a.As&lt;int&gt;(),
    ///     B = b.As&lt;string&gt;(),
    ///     C = c.As&lt;bool&gt;(),
    ///     D = d.As&lt;double&gt;(),
    ///     E = e.As&lt;DateTime&gt;(),
    ///     F = f.As&lt;long&gt;(),
    ///     G = g.As&lt;float&gt;(),
    ///     H = h.As&lt;decimal&gt;(),
    ///     I = i.As&lt;char&gt;(),
    ///     J = j.As&lt;byte&gt;()
    /// });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(
        this IRecord record,
        Expression<Func<object, object, object, object, object, object, object, object, object, object, T>> map)
    {
        return LambdaMapper.Map<T>(record, map);
    }

    /// <summary>
    /// Converts the record to an object using the given lambda expression. This overload cannot be used to map to
    /// anonymous types, as the type must be explicitly specified and cannot be inferred.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="map">A lambda expression that defines the mapping from the record to the result object. The names
    /// of the parameters accepted by the lambda will be used to lookup values in the record, and the values will
    /// be converted to the types of the parameters and then passed to the lambda function. The return type of
    /// this lambda must be assignable to <typeparamref name="T"/>.</param>
    /// <typeparam name="T">The type that will be mapped to and is returned by the lambda.</typeparam>
    /// <example>
    /// This example demonstrates how to use the `AsObject` method to map a record to an instance of a subclass of `SpokenStatement`.
    /// The `AsObject` method is used with a lambda expression that determines the type of the resulting object based on the `isTrue` field of the record.
    /// <code language="csharp">
    /// var record = TestRecord.Create(("isTrue", true), ("description", "This is true"));
    ///
    /// // assuming Fact and Myth are subclasses of SpokenStatement
    /// var spokenStatement = record.AsObject&lt;SpokenStatement&gt;(
    ///     (bool isTrue, string description) => isTrue
    ///         ? (SpokenStatement)new Fact(description)
    ///         : new Myth(description));
    ///
    /// Assert.IsType&lt;Fact&gt;(spokenStatement);
    /// Assert.Equal("This is true", spokenStatement.Description);
    /// </code>
    /// </example>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record, LambdaExpression map)
    {
        return LambdaMapper.Map<T>(record, map);
    }
}
