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
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class ReverseRequestEncapsulatedKeyRepositoryTests
{
    private const string RepositoryId = "repo-1";

    private static readonly byte[] Encapsulation = [1, 2, 3];
    private static readonly Dictionary<string, string> Metadata = new() { ["iv"] = "abc" };

    private readonly Mock<IOutboundRoundTrip> _roundTripMock = new();

    private ReverseRequestEncapsulatedKeyRepository Subject()
    {
        return new ReverseRequestEncapsulatedKeyRepository(_roundTripMock.Object, RepositoryId);
    }

    private IProtocolMessage? _lastRequest;

    private void CaptureRequest<T>(T returning)
    {
        _roundTripMock
            .Setup(r => r.SendExpectingAsync<T>(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(request => _lastRequest = request)
            .ReturnsAsync(returning);
    }

    [Fact]
    public async Task FindByIdAsync_sends_a_find_by_id_request_and_parses_the_reply()
    {
        var wire = new EncapsulatedKeyRepositoryRecord("k1", "a1", Encapsulation, Metadata);
        CaptureRequest<EncapsulatedKeyRepositoryRecord?>(wire);

        var result = await Subject().FindByIdAsync("k1", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryFindByIdRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");

        result.Should().Be(new EncapsulatedKeyRecord("k1", "a1", Encapsulation, Metadata));
    }

    [Fact]
    public void RepositoryId_exposes_the_id_it_was_constructed_with()
    {
        Subject().RepositoryId.Should().Be(RepositoryId);
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_the_repository_has_no_match()
    {
        CaptureRequest<EncapsulatedKeyRepositoryRecord?>(null);

        var result = await Subject().FindByIdAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByAliasAsync_sends_a_find_by_alias_request_and_parses_the_reply()
    {
        var wire = new EncapsulatedKeyRepositoryRecord("k1", "a1", Encapsulation, Metadata);
        CaptureRequest<EncapsulatedKeyRepositoryRecord?>(wire);

        var result = await Subject().FindByAliasAsync("a1", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryFindByAliasRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.Alias.Should().Be("a1");

        result.Should().Be(new EncapsulatedKeyRecord("k1", "a1", Encapsulation, Metadata));
    }

    [Fact]
    public async Task CreateAsync_sends_a_create_request_and_parses_the_reply()
    {
        var wire = new EncapsulatedKeyRepositoryRecord("k1", "a1", Encapsulation, Metadata);
        CaptureRequest(wire);

        var result = await Subject().CreateAsync(
            "a1",
            Encapsulation,
            Metadata,
            TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryCreateRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.Alias.Should().Be("a1");

        result.Should().Be(new EncapsulatedKeyRecord("k1", "a1", Encapsulation, Metadata));
    }

    [Fact]
    public async Task ImportAsync_sends_an_import_request_and_parses_the_reply()
    {
        var wire = new EncapsulatedKeyRepositoryRecord("fixed-id", "a1", Encapsulation, Metadata);
        CaptureRequest(wire);

        var result = await Subject().ImportAsync("fixed-id", "a1", Encapsulation, Metadata);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryImportRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("fixed-id");
        request.Alias.Should().Be("a1");

        result.Should().Be(new EncapsulatedKeyRecord("fixed-id", "a1", Encapsulation, Metadata));
    }

    [Fact]
    public async Task SetAliasByIdAsync_sends_a_set_alias_request()
    {
        CaptureRequest(true);

        await Subject().SetAliasByIdAsync("k1", "a2", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositorySetAliasRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");
        request.Alias.Should().Be("a2");
    }

    [Fact]
    public async Task DeleteByIdAsync_sends_a_delete_request()
    {
        CaptureRequest(true);

        await Subject().DeleteByIdAsync("k1", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryDeleteRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");
    }

    [Fact]
    public void FindByIdCompleted_fulfils_the_expectation_with_the_decoded_record()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new EncapsulatedKeyRepositoryFindByIdCompletedHandler(expectationsMock.Object);
        var wire = new EncapsulatedKeyRepositoryRecord("k1", "a1", Encapsulation, Metadata);
        var message = new EncapsulatedKeyRepositoryFindByIdCompleted { RequestId = "req-1", Record = wire };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("req-1", wire), Times.Once);
    }

    [Fact]
    public void DeleteCompleted_fulfils_the_expectation_with_true()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new EncapsulatedKeyRepositoryDeleteCompletedHandler(expectationsMock.Object);
        var message = new EncapsulatedKeyRepositoryDeleteCompleted { RequestId = "req-1" };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("req-1", true), Times.Once);
    }

    [Theory]
    [InlineData("KeyNotFound", typeof(EncapsulatedKeyNotFoundException))]
    [InlineData("AliasInUse", typeof(EncapsulatedAliasInUseException))]
    public void ErrorCompleted_fails_the_expectation_with_the_matching_exception_type(
        string errorType,
        Type expectedExceptionType)
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new EncapsulatedKeyRepositoryErrorCompletedHandler(expectationsMock.Object);
        var message = new EncapsulatedKeyRepositoryErrorCompleted
        {
            RequestId = "req-1",
            ErrorType = errorType,
            Detail = "k1"
        };

        Exception? failedWith = null;
        expectationsMock.Setup(e => e.Fail("req-1", It.IsAny<Exception>())).Callback<string, Exception>(
            (_, ex) => failedWith = ex);

        handler.ProcessAsync(message);

        failedWith.Should().NotBeNull();
        failedWith!.GetType().Should().Be(expectedExceptionType);
    }
}
