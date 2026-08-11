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
using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class StoredConverterTests
{
    private readonly ObjectStore _objectStore = AutoMocker.ForTesting<ObjectStore>().CreateInstance<ObjectStore>();

    private JsonSerializerOptions Options() =>
        new JsonOptionsProvider([new StoredConverterFactory(_objectStore)]).GetOptions();

    [Fact]
    public void Reads_a_wire_id_by_resolving_it_from_the_objectStore()
    {
        var registered = _objectStore.Register(new Stored());

        var request = JsonSerializer.Deserialize<Request>(
            $$"""{"thingId":"{{registered.Id}}"}""", Options());

        request!.Thing.Object.Should().BeSameAs(registered.Object);
        request.Thing.Id.Should().Be(registered.Id);
    }

    [Fact]
    public void Rejects_a_non_string_wire_id()
    {
        var deserialize = () => JsonSerializer.Deserialize<Request>("""{"thingId":123}""", Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void Rejects_a_null_wire_id()
    {
        var deserialize = () => JsonSerializer.Deserialize<Request>("""{"thingId":null}""", Options());

        deserialize.Should().Throw<TestKitProtocolException>();
    }

    private record Request
    {
        public Stored<Stored> Thing { get; init; } = null!;
    }

    private class Stored;
}
