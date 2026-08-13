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
using Neo4j.Driver.TestKitBackend.Expectations;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record BookmarksSupplierRequest : ICorrelatedRequest
{
    public required string BookmarkManagerId { get; init; }
    public string Id { get; set; } = "";
}

internal record BookmarksSupplierCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required string[] Bookmarks { get; init; }
}

internal class BookmarksSupplierCompletedHandler : MessageHandler<BookmarksSupplierCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public BookmarksSupplierCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(BookmarksSupplierCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Bookmarks);
        return Task.CompletedTask;
    }
}
