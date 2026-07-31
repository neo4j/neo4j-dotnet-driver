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

namespace Neo4j.Driver.TestKitBackend.Retry;

// Bridges the retryable-tx work function (running inside the driver's ExecuteRead/WriteAsync,
// detached from the message loop) and the handlers for whichever request is currently "open" for
// a given session — without the loop ever re-entering itself to read ahead. Two independent
// handshakes, both keyed by sessionId:
//   - outcome: the work function waits on it; RetryablePositiveHandler completes it.
//   - next response: whichever handler is currently waiting for a reply for this session
//     (initially SessionReadTransactionHandler, later RetryablePositiveHandler on each retry
//     round) waits on it; the backgrounded flow completes it once it reaches its next pause point.
internal interface IRetryCoordinator
{
    Task<IProtocolMessage> WaitForNextResponseAsync(string sessionId);

    void CompleteNextResponse(string sessionId, IProtocolMessage response);

    Task WaitForOutcomeAsync(string sessionId);

    void CompleteOutcome(string sessionId);
}
