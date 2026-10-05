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

internal record EncapsulatedKeyRepositoryFindByIdRequest(string RepositoryId, string KeyId) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryFindByIdCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public EncapsulatedKeyRepositoryRecord? Record { get; init; }
}

internal class
    EncapsulatedKeyRepositoryFindByIdCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryFindByIdCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryFindByIdCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryFindByIdCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Record);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositoryFindByAliasRequest(string RepositoryId, string Alias) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryFindByAliasCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public EncapsulatedKeyRepositoryRecord? Record { get; init; }
}

internal class
    EncapsulatedKeyRepositoryFindByAliasCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryFindByAliasCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryFindByAliasCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryFindByAliasCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Record);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositoryCreateRequest(
    string RepositoryId,
    string? Alias,
    HexBytes Encapsulation,
    IReadOnlyDictionary<string, string> Metadata) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryCreateCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required EncapsulatedKeyRepositoryRecord Record { get; init; }
}

internal class
    EncapsulatedKeyRepositoryCreateCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryCreateCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryCreateCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryCreateCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Record);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositoryImportRequest(
    string RepositoryId,
    string KeyId,
    string Alias,
    HexBytes Encapsulation,
    IReadOnlyDictionary<string, string> Metadata) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryImportCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required EncapsulatedKeyRepositoryRecord Record { get; init; }
}

internal class
    EncapsulatedKeyRepositoryImportCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryImportCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryImportCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryImportCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Record);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositorySetAliasRequest(string RepositoryId, string KeyId, string? Alias)
    : IProtocolMessage;

internal record EncapsulatedKeyRepositorySetAliasCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
}

internal class
    EncapsulatedKeyRepositorySetAliasCompletedHandler : MessageHandler<EncapsulatedKeyRepositorySetAliasCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositorySetAliasCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositorySetAliasCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, true);
        return Task.CompletedTask;
    }
}

internal record EncapsulatedKeyRepositoryDeleteRequest(string RepositoryId, string KeyId) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryDeleteCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
}

internal class
    EncapsulatedKeyRepositoryDeleteCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryDeleteCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryDeleteCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryDeleteCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, true);
        return Task.CompletedTask;
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
            "AliasNotFound" => new EncapsulatedAliasNotFoundException(detail),
            "AliasInUse" => new EncapsulatedAliasInUseException(detail),
            _ => new EncapsulatedKeyRepositoryException($"Unknown repository error '{message.ErrorType}'.")
        };
    }
}

/// <summary>
/// Tells the testkit frontend that a repository's owning driver has closed, so it can drop that
/// repository's storage. Sent by <c>DriverCloseHandler</c>.
/// </summary>
internal record EncapsulatedKeyRepositoryClosed(string RepositoryId) : IProtocolMessage;

internal record EncapsulatedKeyRepositoryClosedCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
}

internal class
    EncapsulatedKeyRepositoryClosedCompletedHandler : MessageHandler<EncapsulatedKeyRepositoryClosedCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public EncapsulatedKeyRepositoryClosedCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(EncapsulatedKeyRepositoryClosedCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, true);
        return Task.CompletedTask;
    }
}
