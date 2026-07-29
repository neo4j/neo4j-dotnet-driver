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

using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Protocol;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record StartTestRequest : IProtocolMessage
{
    public string TestName { get; init; } = "";
}

internal record RunTestResponse : IProtocolMessage;

internal record SkipTestResponse(string Reason) : IProtocolMessage;

internal interface ISkipPolicy
{
    bool TryGetSkipReason(string testName, out string reason);
}

internal class StartTestHandler : MessageHandler<StartTestRequest>
{
    private readonly ILoggingContext _loggingContext;
    private readonly ISkipPolicy _skipPolicy;

    public StartTestHandler(ILoggingContext loggingContext, ISkipPolicy skipPolicy)
    {
        _loggingContext = loggingContext;
        _skipPolicy = skipPolicy;
    }

    public override Task<IProtocolMessage?> ProcessAsync(StartTestRequest message)
    {
        _loggingContext.Set("test", message.TestName);

        IProtocolMessage response = _skipPolicy.TryGetSkipReason(message.TestName, out var reason)
            ? new SkipTestResponse(reason)
            : new RunTestResponse();

        return Task.FromResult<IProtocolMessage?>(response);
    }
}
