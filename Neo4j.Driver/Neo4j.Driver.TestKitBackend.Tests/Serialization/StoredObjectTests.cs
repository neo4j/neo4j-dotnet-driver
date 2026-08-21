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

using FluentAssertions;
using Moq;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Serialization;

public class StoredObjectTests
{
    public interface IFakeThing;

    private record ObjectOnlyRequest : IProtocolMessage
    {
        [StoredObject]
        public required IFakeThing Thing { get; init; }
    }

    private record BothRequest : IProtocolMessage
    {
        [StoredObject]
        public required IFakeThing Thing { get; init; }
        public required string ThingId { get; init; }
    }

    private record IdOnlyRequest : IProtocolMessage
    {
        public required string ThingId { get; init; }
    }

    private record RenamedRequest : IProtocolMessage
    {
        [StoredObject("customRef")]
        public required IFakeThing Thing { get; init; }
    }

    private readonly Mock<IObjectStoreAccessor> _objectStoreMock = new();
    private readonly IFakeThing _thing = Mock.Of<IFakeThing>();

    private IMessageSerializer Serializer()
    {
        var typeMapMock = new Mock<IMessageTypeMap>();
        typeMapMock.Setup(m => m.GetTypeByName("ObjectOnly")).Returns(typeof(ObjectOnlyRequest));
        typeMapMock.Setup(m => m.GetTypeByName("Both")).Returns(typeof(BothRequest));
        typeMapMock.Setup(m => m.GetTypeByName("IdOnly")).Returns(typeof(IdOnlyRequest));
        typeMapMock.Setup(m => m.GetTypeByName("Renamed")).Returns(typeof(RenamedRequest));

        var envelopeConverter = new EnvelopeConverter(typeMapMock.Object, new StoredObjectFieldTransformer());
        return new MessageSerializer(
            new JsonOptionsProvider(
                [envelopeConverter],
                _objectStoreMock.Object));
    }

    private void StoreThing(string id)
    {
        _objectStoreMock
            .Setup(s => s.Get<IFakeThing>(id))
            .Returns(_thing);
    }

    [Fact]
    public void An_object_only_property_resolves_from_the_id_field()
    {
        StoreThing("t-1");

        var message = Serializer().Deserialize("""{"name":"ObjectOnly","data":{"thingId":"t-1"}}""");

        message.Should().BeOfType<ObjectOnlyRequest>().Which.Thing.Should().BeSameAs(_thing);
    }

    [Fact]
    public void An_object_and_an_id_property_both_populate_from_the_one_wire_field()
    {
        StoreThing("t-1");

        var message = Serializer().Deserialize("""{"name":"Both","data":{"thingId":"t-1"}}""");

        var request = message.Should().BeOfType<BothRequest>().Subject;
        request.Thing.Should().BeSameAs(_thing);
        request.ThingId.Should().Be("t-1");
    }

    [Fact]
    public void An_id_only_property_binds_plainly_without_a_store_lookup()
    {
        var message = Serializer().Deserialize("""{"name":"IdOnly","data":{"thingId":"t-1"}}""");

        message.Should().BeOfType<IdOnlyRequest>().Which.ThingId.Should().Be("t-1");
        _objectStoreMock.Verify(s => s.Get<IFakeThing>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void A_missing_id_field_fails_the_object_property_requiredness()
    {
        var act = () => Serializer().Deserialize("""{"name":"ObjectOnly","data":{}}""");

        act.Should().Throw<TestKitProtocolException>();
    }

    [Fact]
    public void An_unknown_id_surfaces_the_store_error()
    {
        _objectStoreMock
            .Setup(s => s.Get<IFakeThing>("missing"))
            .Throws(new TestKitProtocolException("No object is stored with id 'missing'."));

        var act = () => Serializer().Deserialize("""{"name":"ObjectOnly","data":{"thingId":"missing"}}""");

        act.Should().Throw<TestKitProtocolException>().WithMessage("*missing*");
    }

    [Fact]
    public void The_attribute_name_argument_overrides_the_id_field_name()
    {
        StoreThing("t-9");

        var message = Serializer().Deserialize("""{"name":"Renamed","data":{"customRef":"t-9"}}""");

        message.Should().BeOfType<RenamedRequest>().Which.Thing.Should().BeSameAs(_thing);
    }
}
