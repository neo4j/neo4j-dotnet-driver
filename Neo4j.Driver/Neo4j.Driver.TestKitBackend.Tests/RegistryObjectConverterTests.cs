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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class RegistryObjectConverterTests
{
    private readonly Registry _registry = new();

    private JsonSerializerOptions Options() =>
        new JsonOptionsProvider([new RegistryObjectConverterFactory(_registry)]).GetOptions();

    [Fact]
    public void Reads_a_wire_id_by_resolving_it_from_the_registry()
    {
        var registered = _registry.Register(new Stored());

        var request = JsonSerializer.Deserialize<Request>(
            $$"""{"thingId":"{{registered.Id}}"}""", Options());

        request!.Thing.Object.Should().BeSameAs(registered.Object);
        request.Thing.Id.Should().Be(registered.Id);
    }

    [Fact]
    public void Writes_the_id_as_a_plain_json_string()
    {
        var registered = _registry.Register(new Stored());

        var json = JsonSerializer.Serialize(new Response { Id = registered }, Options());

        json.Should().Be($$"""{"id":"{{registered.Id}}"}""");
    }

    private record Request
    {
        public RegistryObject<Stored> Thing { get; init; } = null!;
    }

    private record Response
    {
        public RegistryObject<Stored> Id { get; init; } = null!;
    }

    private class Stored;
}
