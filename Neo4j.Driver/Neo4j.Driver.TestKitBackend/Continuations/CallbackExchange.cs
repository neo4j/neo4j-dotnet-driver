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
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Continuations;

internal interface ICallbackRequest : IProtocolMessage;

internal interface ICallbackCompletion : IProtocolMessage
{
    string RequestId { get; }
}

internal interface ICallbackExchange
{
    Task<TCompletion> SendAsync<TCompletion>(Func<string, ICallbackRequest> createRequest)
        where TCompletion : ICallbackCompletion;
}

internal class CallbackExchange : ICallbackExchange
{
    private readonly IResponseWriter _responseWriter;
    private readonly IConnectionInput _input;
    private readonly IMessageSerializer _serializer;

    public CallbackExchange(IResponseWriter responseWriter, IConnectionInput input, IMessageSerializer serializer)
    {
        _responseWriter = responseWriter;
        _input = input;
        _serializer = serializer;
    }

    public async Task<TCompletion> SendAsync<TCompletion>(Func<string, ICallbackRequest> createRequest)
        where TCompletion : ICallbackCompletion
    {
        var requestId = Guid.NewGuid().ToString();
        await _responseWriter.WriteAsync(createRequest(requestId));

        var json = await _input.ReadRequestAsync() ??
            throw new InvalidOperationException(
                $"Connection closed while awaiting a {typeof(TCompletion).Name} callback completion.");

        var message = _serializer.Deserialize(json);
        if (message is not TCompletion completion)
        {
            throw new InvalidOperationException(
                $"Expected a {typeof(TCompletion).Name} callback completion but received " +
                $"{message.GetType().Name}.");
        }

        if (completion.RequestId != requestId)
        {
            throw new InvalidOperationException(
                $"Callback completion request id '{completion.RequestId}' did not match the expected id " +
                $"'{requestId}'.");
        }

        return completion;
    }
}
