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

using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record EncapsulatedKeyRepositoryRecord(
    string Id,
    string? Alias,
    HexBytes Encapsulation,
    IReadOnlyDictionary<string, string> Metadata)
{
    public EncapsulatedKeyRecord ToRecord()
    {
        return new EncapsulatedKeyRecord(Id, Alias, Encapsulation, Metadata);
    }
}

internal interface IEncapsulatedKeyRepositoryReply : IProtocolMessage
{
    string RequestId { get; }
}

internal abstract class EncapsulatedKeyRepositoryReplyHandler<TReply> : MessageHandler<TReply>
    where TReply : IEncapsulatedKeyRepositoryReply
{
    private readonly IExpectationStore _expectationStore;

    protected EncapsulatedKeyRepositoryReplyHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(TReply message)
    {
        _expectationStore.Fulfil(message.RequestId, message);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositoryFindByIdRequest(string RepositoryId, string KeyId) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryFindByIdCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
    public EncapsulatedKeyRepositoryRecord? Record { get; init; }
}

internal class EncapsulatedKeyRepositoryFindByIdCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositoryFindByIdCompleted>
{
    public EncapsulatedKeyRepositoryFindByIdCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositoryFindByAliasRequest(string RepositoryId, string Alias) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryFindByAliasCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
    public EncapsulatedKeyRepositoryRecord? Record { get; init; }
}

internal class EncapsulatedKeyRepositoryFindByAliasCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositoryFindByAliasCompleted>
{
    public EncapsulatedKeyRepositoryFindByAliasCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositoryCreateRequest(
    string RepositoryId,
    string? Alias,
    HexBytes Encapsulation,
    IReadOnlyDictionary<string, string> Metadata) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryCreateCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
    public required EncapsulatedKeyRepositoryRecord Record { get; init; }
}

internal class EncapsulatedKeyRepositoryCreateCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositoryCreateCompleted>
{
    public EncapsulatedKeyRepositoryCreateCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositoryImportRequest(
    string RepositoryId,
    string KeyId,
    string Alias,
    HexBytes Encapsulation,
    IReadOnlyDictionary<string, string> Metadata) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryImportCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
    public required EncapsulatedKeyRepositoryRecord Record { get; init; }
}

internal class EncapsulatedKeyRepositoryImportCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositoryImportCompleted>
{
    public EncapsulatedKeyRepositoryImportCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositorySetAliasRequest(string RepositoryId, string KeyId, string? Alias)
    : IProtocolMessage;

internal record EncapsulatedKeyRepositorySetAliasCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
}

internal class EncapsulatedKeyRepositorySetAliasCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositorySetAliasCompleted>
{
    public EncapsulatedKeyRepositorySetAliasCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositoryDeleteRequest(string RepositoryId, string KeyId) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryDeleteCompleted : IEncapsulatedKeyRepositoryReply
{
    public required string RequestId { get; init; }
}

internal class EncapsulatedKeyRepositoryDeleteCompletedHandler
    : EncapsulatedKeyRepositoryReplyHandler<EncapsulatedKeyRepositoryDeleteCompleted>
{
    public EncapsulatedKeyRepositoryDeleteCompletedHandler(IExpectationStore expectationStore)
        : base(expectationStore)
    {
    }
}

internal record EncapsulatedKeyRepositoryErrorCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required string ErrorType { get; init; }
    public string? Detail { get; init; }
}

internal class EncapsulatedKeyRepositoryErrorCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryErrorCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryErrorCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryErrorCompleted message)
    {
        _expectationStore.Fail(message.RequestId, ToException(message));
        return Task.CompletedTask;
    }

    private static Exception ToException(EncapsulatedKeyRepositoryErrorCompleted message)
    {
        var detail = message.Detail ?? "";
        return message.ErrorType switch
        {
            "KeyNotFound" => new EncapsulatedKeyNotFoundException(detail),
            "AliasInUse" => new EncapsulatedAliasInUseException(detail),
            _ => new EncapsulatedKeyRepositoryException($"Unknown repository error '{message.ErrorType}': {detail}")
        };
    }
}
