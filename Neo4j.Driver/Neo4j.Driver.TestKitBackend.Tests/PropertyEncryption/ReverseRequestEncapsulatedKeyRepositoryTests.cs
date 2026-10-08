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
    private readonly Mock<IExpectationStore> _expectationStoreMock = new();
    private IProtocolMessage? _lastRequest;

    private ReverseRequestEncapsulatedKeyRepository Subject()
    {
        return new ReverseRequestEncapsulatedKeyRepository(_roundTripMock.Object, RepositoryId);
    }

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
        CaptureRequest(new EncapsulatedKeyRepositoryFindByIdCompleted { RequestId = "req-1", Record = wire });

        var result = await Subject().FindByIdAsync("k1", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryFindByIdRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");

        result.Should().Be(new EncapsulatedKeyRecord("k1", "a1", Encapsulation, Metadata));
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_the_repository_has_no_match()
    {
        CaptureRequest(new EncapsulatedKeyRepositoryFindByIdCompleted { RequestId = "req-1" });

        var result = await Subject().FindByIdAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByAliasAsync_sends_a_find_by_alias_request_and_parses_the_reply()
    {
        var wire = new EncapsulatedKeyRepositoryRecord("k1", "a1", Encapsulation, Metadata);
        CaptureRequest(new EncapsulatedKeyRepositoryFindByAliasCompleted { RequestId = "req-1", Record = wire });

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
        CaptureRequest(new EncapsulatedKeyRepositoryCreateCompleted { RequestId = "req-1", Record = wire });

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
        CaptureRequest(new EncapsulatedKeyRepositoryImportCompleted { RequestId = "req-1", Record = wire });

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
        CaptureRequest(new EncapsulatedKeyRepositorySetAliasCompleted { RequestId = "req-1" });

        await Subject().SetAliasByIdAsync("k1", "a2", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositorySetAliasRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");
        request.Alias.Should().Be("a2");
    }

    [Fact]
    public async Task DeleteByIdAsync_sends_a_delete_request()
    {
        CaptureRequest(new EncapsulatedKeyRepositoryDeleteCompleted { RequestId = "req-1" });

        await Subject().DeleteByIdAsync("k1", TestContext.Current.CancellationToken);

        var request = _lastRequest.Should().BeOfType<EncapsulatedKeyRepositoryDeleteRequest>().Subject;
        request.RepositoryId.Should().Be(RepositoryId);
        request.KeyId.Should().Be("k1");
    }

    [Theory]
    [InlineData("KeyNotFound", typeof(EncapsulatedKeyNotFoundException))]
    [InlineData("AliasInUse", typeof(EncapsulatedAliasInUseException))]
    public async Task ErrorCompleted_fails_the_expectation_with_the_matching_exception_type(
        string errorType,
        Type expectedExceptionType)
    {
        var handler = new EncapsulatedKeyRepositoryErrorCompletedHandler(_expectationStoreMock.Object);
        var message = new EncapsulatedKeyRepositoryErrorCompleted
        {
            RequestId = "req-1",
            ErrorType = errorType,
            Detail = "k1"
        };

        Exception? failedWith = null;
        _expectationStoreMock.Setup(e => e.Fail("req-1", It.IsAny<Exception>()))
            .Callback<string, Exception>((_, ex) => failedWith = ex);

        await handler.ProcessAsync(message);

        failedWith.Should().BeOfType(expectedExceptionType);
    }

    [Fact]
    public async Task ErrorCompleted_with_an_unknown_error_type_keeps_the_detail()
    {
        var handler = new EncapsulatedKeyRepositoryErrorCompletedHandler(_expectationStoreMock.Object);
        var message = new EncapsulatedKeyRepositoryErrorCompleted
        {
            RequestId = "req-1",
            ErrorType = "UnknownRepository",
            Detail = "repo-9"
        };

        Exception? failedWith = null;
        _expectationStoreMock.Setup(e => e.Fail("req-1", It.IsAny<Exception>()))
            .Callback<string, Exception>((_, ex) => failedWith = ex);

        await handler.ProcessAsync(message);

        failedWith.Should().BeOfType<EncapsulatedKeyRepositoryException>()
            .Which.Message.Should().Contain("UnknownRepository").And.Contain("repo-9");
    }
}
