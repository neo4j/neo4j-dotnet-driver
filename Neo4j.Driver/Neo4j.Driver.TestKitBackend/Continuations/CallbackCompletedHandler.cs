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

namespace Neo4j.Driver.TestKitBackend.Continuations;

// A testkit reply to a backend → testkit callback request (spec §6), carrying back the
// correlation id the callback request was sent with. Implementing this is all a completion
// message needs — BackendModule wires each implementor to the shared CallbackCompletedHandler.
internal interface ICallbackCompletion : IProtocolMessage
{
    string RequestId { get; }
}

// Shared handler for every callback completion: resolves the paused driver-side callback with
// the completion, then holds the response slot for whatever the resumed operation produces next
// (its terminal response or another callback request), per spec §6. It has no direct response
// of its own.
internal class CallbackCompletedHandler<T> : MessageHandler<T> where T : ICallbackCompletion
{
    private readonly IContinuationCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;

    public CallbackCompletedHandler(IContinuationCoordinator coordinator, IResponseWriter responseWriter)
    {
        _coordinator = coordinator;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(T message)
    {
        var responseTask = _coordinator.WaitForNextResponseAsync();
        _coordinator.CompleteCallback(message.RequestId, message);
        await _responseWriter.WriteAsync(await responseTask);
    }
}
