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

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Neo4j.Driver.Internal.QueryApi;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

internal static class QueryApiCodecAssert
{
    internal sealed class NullValue
    {
    }

    private static readonly string[] AllTypeNames =
    [
        "Null", "Boolean", "Integer", "Float", "String", "Base64", "Unsupported",
        "List", "Map",
        "Vector",
        "Date", "Time", "LocalTime", "ZonedDateTime", "OffsetDateTime", "LocalDateTime", "Duration",
        "Point", "Node", "Relationship", "Path"
    ];

    private static readonly (Type Key, object? Value)[] AllWriteSamples =
    [
        (typeof(NullValue), null),
        (typeof(bool), true),
        (typeof(long), 42L),
        (typeof(int), 7),
        (typeof(short), (short)1),
        (typeof(sbyte), (sbyte)1),
        (typeof(double), 3.14),
        (typeof(float), 1.5f),
        (typeof(string), "hi"),
        (typeof(byte[]), new byte[] { 1, 2, 3 }),
        (typeof(List<object?>), new List<object?>()),
        (typeof(object[]), new object?[] { 3.14, "📘", false }),
        (typeof(IDictionary<string, object?>), new Dictionary<string, object?>())
    ];

    public static void CanRead(IQueryApiTypeCodec codec, params string[] owned)
    {
        var readableTypeNames = new HashSet<string>(owned, StringComparer.Ordinal);
        foreach (var name in AllTypeNames)
        {
            var expected = readableTypeNames.Contains(name);

            codec.CanRead(name)
                .Should()
                .Be(
                    expected,
                    $"CanRead(\"{name}\") should be {expected} for {codec.GetType().Name}");
        }
    }

    public static void CanWrite(IQueryApiTypeCodec codec, params Type[] owned)
    {
        var writableTypes = new HashSet<Type>(owned);
        foreach (var (type, sampleValue) in AllWriteSamples)
        {
            var expected = writableTypes.Contains(type);

            codec.CanWrite(sampleValue)
                .Should()
                .Be(
                    expected,
                    $"CanWrite({type.Name}) should be {expected} for {codec.GetType().Name}");
        }
    }

    public static AndConstraint<ObjectAssertions> BeTypedEnvelope<T>(
        this ObjectAssertions assertions,
        string expectedType,
        T expectedValue,
        string because = "",
        params object[] becauseArgs)
    {
        if (assertions.Subject is not JsonNode node)
        {
            throw new ArgumentException("Expected a JsonNode");
        }

        var type = node?["$type"]?.GetValue<string>() ?? "<null type>";
        var maybeValue = node?["_value"];
        var value = maybeValue is null ? default! : maybeValue.GetValue<T>();

        Execute.Assertion.ForCondition(type == expectedType && value?.Equals(expectedValue) == true)
            .BecauseOf(because, becauseArgs)
            .WithDefaultIdentifier("JsonNode")
            .FailWith(
                "Expected a typed envelope with type {0} and value {1}, but found {2}",
                expectedType,
                expectedValue,
                node?.ToJsonString() ?? "<null>");

        return new AndConstraint<ObjectAssertions>(assertions);
    }
}
