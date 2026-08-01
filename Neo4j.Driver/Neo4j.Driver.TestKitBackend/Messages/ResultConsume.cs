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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Summary;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ResultConsumeRequest : IProtocolMessage
{
    public required RegistryObject<IResultCursor> Result { get; init; }
}

internal record SummaryResponse(
    SummaryQueryResponse Query,
    string? QueryType,
    SummaryPlanResponse? Plan,
    SummaryProfileResponse? Profile,
    IReadOnlyList<SummaryNotificationResponse> Notifications,
    string? Database,
    SummaryServerInfoResponse ServerInfo,
    SummaryCountersResponse Counters,
    long? ResultAvailableAfter,
    long? ResultConsumedAfter,
    IReadOnlyList<SummaryGqlStatusObjectResponse> GqlStatusObjects) : IProtocolMessage;

internal class ResultConsumeHandler : DetachedOperationHandler<ResultConsumeRequest>
{
    private readonly ISummaryMapper _summaryMapper;

    public ResultConsumeHandler(
        ISummaryMapper summaryMapper,
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, logger)
    {
        _summaryMapper = summaryMapper;
    }

    protected override async Task<IProtocolMessage> ExecuteAsync(ResultConsumeRequest message)
    {
        var summary = await message.Result.Object.ConsumeAsync();
        return _summaryMapper.Map(summary);
    }
}
