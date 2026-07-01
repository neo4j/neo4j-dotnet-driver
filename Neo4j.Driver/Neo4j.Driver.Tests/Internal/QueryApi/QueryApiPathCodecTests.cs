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

using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiPathCodecTests
{
    private readonly QueryApiPathCodec _subject = new();

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Path");

    [Fact]
    public void CanWrite_FalseForAllTypes() => CanWrite(_subject);

    [Fact]
    public void Write_Throws()
    {
        _subject
            .Invoking(s => s.Write(null, Mock.Of<IJsonValueEncoder>()))
            .Should()
            .Throw<System.NotSupportedException>();
    }

    [Fact]
    public void Read_PathWithNoRelationships_HasTheSingleNodeAsBothStartAndEnd()
    {
        // A zero-length path is a single node wrapped in the array, no relationships.
        using var wire = PathWire("(alice)");

        var alice = Mock.Of<INode>();
        var decoder = SetupDecoderMock(("(alice)", alice));

        var path = (IPath)_subject.Read(wire.RootElement, decoder)!;

        path.Nodes.Should().ContainSingle().Which.Should().BeSameAs(alice);
        path.Relationships.Should().BeEmpty();
        path.Start.Should().BeSameAs(alice);
        path.End.Should().BeSameAs(alice);
    }

    [Fact]
    public void Read_PathWithTwoRelationships_ReconstructsNodesAndRelationshipsInTraversalOrder()
    {
        // (alice)-[knows]->(bob)-[worksWith]->(carol)
        using var wire = PathWire("(alice)", "[knows]", "(bob)", "[worksWith]", "(carol)");

        var alice = Mock.Of<INode>();
        var bob = Mock.Of<INode>();
        var carol = Mock.Of<INode>();
        var knows = Mock.Of<IRelationship>();
        var worksWith = Mock.Of<IRelationship>();

        var decoder = SetupDecoderMock(
            ("(alice)", alice),
            ("[knows]", knows),
            ("(bob)", bob),
            ("[worksWith]", worksWith),
            ("(carol)", carol));

        var path = (IPath)_subject.Read(wire.RootElement, decoder)!;

        path.Nodes.Should().ContainInOrder(alice, bob, carol).And.HaveCount(3);
        path.Relationships.Should().ContainInOrder(knows, worksWith).And.HaveCount(2);
        path.Start.Should().BeSameAs(alice);
        path.End.Should().BeSameAs(carol);
    }

    // build a Path envelope whose _value array holds the given element labels
    private static JsonDocument PathWire(params string[] elementLabels)
    {
        var elements = string.Join(", ", elementLabels.Select(label => $"\"{label}\""));
        return JsonDocument.Parse($$"""{ "$type": "Path", "_value": [ {{elements}} ] }""");
    }

    private static IJsonValueDecoder SetupDecoderMock(params (string Label, object GraphObject)[] mappings)
    {
        var decoder = new Mock<IJsonValueDecoder>();
        foreach (var (label, graphObject) in mappings)
        {
            var expectedLabel = label;
            decoder
                .Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetString() == expectedLabel)))
                .Returns(graphObject);
        }

        return decoder.Object;
    }
}
