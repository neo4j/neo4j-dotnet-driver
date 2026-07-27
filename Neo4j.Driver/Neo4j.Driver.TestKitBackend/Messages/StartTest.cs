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

internal record StartTest : IProtocolMessage
{
    public string TestName { get; init; } = "";
}

internal record RunTest : IProtocolMessage;

internal class StartTestHandler : MessageHandler<StartTest>
{
    private readonly ILoggingContext _loggingContext;

    public StartTestHandler(ILoggingContext loggingContext)
    {
        _loggingContext = loggingContext;
    }

    public override Task<IProtocolMessage?> ProcessAsync(StartTest message)
    {
        _loggingContext.Set("TestName", message.TestName);

        // Always run for now; a skip policy (blacklist) comes later.
        return Task.FromResult<IProtocolMessage?>(new RunTest());
    }
}
