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

using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ResultConsumeRequest : IProtocolMessage
{
    public required RegistryObject<IResultCursor> Result { get; init; }
}

internal class ResultConsumeHandler : MessageHandler<ResultConsumeRequest>
{
    public override async Task ProcessAsync(ResultConsumeRequest message)
    {
        await message.Result.Object.ConsumeAsync();

        // A driver exception during consume propagates past this point (handled by MessageLoop).
        // Building a Summary response for the success path is separate, not-yet-built scope.
        throw new NotImplementedException("Summary construction is not yet implemented");
    }
}
