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

namespace Neo4j.Driver.TestKitBackend.Dispatch;

internal class MessageDispatcher : IMessageDispatcher
{
    private readonly IReadOnlyDictionary<Type, Func<IMessageHandler>> _handlerFactories;

    public MessageDispatcher(Func<IMessageHandler>[] handlerFactories)
    {
        _handlerFactories = handlerFactories
            .ToDictionary(f => MessageHandlingHelper.MessageTypeFor(f().GetType()));
    }

    public Task DispatchAsync(IProtocolMessage message)
    {
        if (!_handlerFactories.TryGetValue(message.GetType(), out var factory))
        {
            throw new UnknownMessageException(message.GetType());
        }

        return factory().ProcessAsync(message);
    }
}
