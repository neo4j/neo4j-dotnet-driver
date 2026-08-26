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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class CorrelatedRequestWrapperTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EnvelopeConverter>();

    public CorrelatedRequestWrapperTests()
    {
        _autoMocker.Use<IStoredObjectFieldTransformer>(new StoredObjectFieldTransformer());
    }

    private JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                _autoMocker.CreateInstance<EnvelopeConverter>(),
                new CorrelatedRequestWrapperConverter()
            }
        };
    }

    [Fact]
    public void Writes_the_inner_messages_data_with_the_id_added()
    {
        var correlated = new CorrelatedRequestWrapper(new SamplePrompt { Value = "x" }, "id-1");

        var json = JsonSerializer.Serialize<IProtocolMessage>(correlated, Options());

        json.Should().Be("""{"name":"SamplePrompt","data":{"value":"x","id":"id-1"}}""");
    }

    [Fact]
    public void Writes_the_inner_messages_outbound_name_not_the_wrappers()
    {
        var correlated = new CorrelatedRequestWrapper(new SampleResponse { Value = "y" }, "id-2");

        var json = JsonSerializer.Serialize<IProtocolMessage>(correlated, Options());

        json.Should().Be("""{"name":"Sample","data":{"value":"y","id":"id-2"}}""");
    }

    private record SamplePrompt : IProtocolMessage
    {
        public string Value { get; init; } = "";
    }

    private record SampleResponse : IProtocolMessage
    {
        public string Value { get; init; } = "";
    }
}
