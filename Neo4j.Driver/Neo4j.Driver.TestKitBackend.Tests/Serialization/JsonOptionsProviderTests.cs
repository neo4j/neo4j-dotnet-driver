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

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Moq;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class JsonOptionsProviderTests
{
    [Fact]
    public void GetOptions_applies_the_conventions_and_adds_the_injected_converters()
    {
        var converter = new SampleConverter();
        var provider = new JsonOptionsProvider([converter], Mock.Of<IObjectStoreAccessor>());

        var options = provider.GetOptions();

        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        options.UnmappedMemberHandling.Should().Be(JsonUnmappedMemberHandling.Disallow);
        options.Converters.Should().Contain(converter);
    }

    private class SampleConverter : JsonConverter<string>, IProtocolJsonConverter
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }

    public class RealMessageTypesUnderTheStrictBasePreset
    {
        private readonly Mock<IObjectStoreAccessor> _objectStoreMock = new();

        private JsonSerializerOptions RealOptions()
        {
            return new JsonOptionsProvider(
                [new OptionalConverterFactory()],
                _objectStoreMock.Object)
                .GetOptions();
        }

        private SessionRunRequest DeserializeSessionRun(string json)
        {
            var session = Mock.Of<IAsyncSession>();
            _objectStoreMock
                .Setup(s => s.Get<IAsyncSession>("session-1"))
                .Returns(session);

            return JsonSerializer.Deserialize<SessionRunRequest>(json, RealOptions())!;
        }

        [Fact]
        public void A_stored_object_property_resolves_through_the_scopes_ObjectStore()
        {
            var request = DeserializeSessionRun("""{"session":"session-1","cypher":"RETURN 1"}""");

            request.Session.Should().NotBeNull();
        }

        [Fact]
        public void An_absent_Optional_field_is_not_specified()
        {
            var request = DeserializeSessionRun("""{"session":"session-1","cypher":"RETURN 1"}""");

            request.Timeout.IsSpecified(out _).Should().BeFalse();
        }

        [Fact]
        public void An_explicit_null_Optional_field_is_specified_as_null()
        {
            var request = DeserializeSessionRun(
                """{"session":"session-1","cypher":"RETURN 1","timeout":null}""");

            request.Timeout.IsSpecified(out var value).Should().BeTrue();
            value.Should().BeNull();
        }

        [Fact]
        public void A_present_numeric_Optional_field_is_specified_with_its_value()
        {
            var request = DeserializeSessionRun(
                """{"session":"session-1","cypher":"RETURN 1","timeout":5000}""");

            request.Timeout.IsSpecified(out var value).Should().BeTrue();
            value.Should().Be(5000L);
        }

        [Fact]
        public void A_missing_required_field_throws()
        {
            var read = () => DeserializeSessionRun("""{"session":"session-1"}""");

            read.Should().Throw<JsonException>();
        }

        [Fact]
        public void An_explicit_null_for_a_required_non_nullable_field_throws()
        {
            var read = () => DeserializeSessionRun("""{"session":"session-1","cypher":null}""");

            read.Should().Throw<JsonException>();
        }
    }
}
