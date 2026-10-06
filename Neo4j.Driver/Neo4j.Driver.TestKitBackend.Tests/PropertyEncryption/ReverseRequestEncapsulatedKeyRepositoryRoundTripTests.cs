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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class ReverseRequestEncapsulatedKeyRepositoryRoundTripTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ReverseRequestEncapsulatedKeyRepository>();
    private readonly IExpectationStore _expectationStore;

    public ReverseRequestEncapsulatedKeyRepositoryRoundTripTests()
    {
        _expectationStore = _autoMocker.CreateInstance<ExpectationStore>();
        _autoMocker.Use(_expectationStore);
        _autoMocker.Use<IOutboundRoundTrip>(_autoMocker.CreateInstance<OutboundRoundTrip>());
    }

    private void ReplyToEachRequestWith(Func<string, Task> reply)
    {
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Returns<IProtocolMessage>(message => reply(((CorrelatedRequestWrapper)message).Id));
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_the_frontend_replies_with_no_record()
    {
        var handler = new EncapsulatedKeyRepositoryFindByIdCompletedHandler(_expectationStore);
        ReplyToEachRequestWith(
            requestId => handler.ProcessAsync(new EncapsulatedKeyRepositoryFindByIdCompleted { RequestId = requestId }));

        var result = await _autoMocker.CreateInstance<ReverseRequestEncapsulatedKeyRepository>()
            .FindByIdAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByAliasAsync_returns_null_when_the_frontend_replies_with_no_record()
    {
        var handler = new EncapsulatedKeyRepositoryFindByAliasCompletedHandler(_expectationStore);
        ReplyToEachRequestWith(
            requestId => handler.ProcessAsync(
                new EncapsulatedKeyRepositoryFindByAliasCompleted { RequestId = requestId }));

        var result = await _autoMocker.CreateInstance<ReverseRequestEncapsulatedKeyRepository>()
            .FindByAliasAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
