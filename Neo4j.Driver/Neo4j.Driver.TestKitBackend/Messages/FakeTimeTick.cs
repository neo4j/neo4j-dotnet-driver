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

using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Time;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record FakeTimeTickRequest : IProtocolMessage
{
    public required long IncrementMs { get; init; }
}

internal class FakeTimeTickHandler : MessageHandler<FakeTimeTickRequest>
{
    private readonly IFakeTimeService _fakeTime;
    private readonly IResponseWriter _responseWriter;

    public FakeTimeTickHandler(IFakeTimeService fakeTime, IResponseWriter responseWriter)
    {
        _fakeTime = fakeTime;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(FakeTimeTickRequest message)
    {
        _fakeTime.Tick(message.IncrementMs);
        await _responseWriter.WriteAsync(new FakeTimeAckResponse());
    }
}
