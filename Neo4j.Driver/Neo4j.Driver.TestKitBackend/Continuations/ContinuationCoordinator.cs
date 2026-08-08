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

namespace Neo4j.Driver.TestKitBackend.Continuations;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class ContinuationCoordinator : IContinuationCoordinator
{
    private readonly Dictionary<string, TaskCompletionSource> _pendingOutcomes = new();
    private TaskCompletionSource<IProtocolMessage>? _pendingResponse;

    public Task<IProtocolMessage> WaitForNextResponseAsync()
    {
        if (_pendingResponse is not null)
        {
            throw new InvalidOperationException("A response continuation is already registered.");
        }

        _pendingResponse = new TaskCompletionSource<IProtocolMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _pendingResponse.Task;
    }

    public void CompleteNextResponse(IProtocolMessage response)
    {
        if (_pendingResponse is null)
        {
            throw new InvalidOperationException("No pending response continuation is registered.");
        }

        var tcs = _pendingResponse;
        _pendingResponse = null;
        tcs.SetResult(response);
    }

    public void CancelNextResponse()
    {
        _pendingResponse = null;
    }

    public Task WaitForOutcomeAsync(string sessionId)
    {
        if (_pendingOutcomes.ContainsKey(sessionId))
        {
            throw new InvalidOperationException(
                $"An outcome continuation is already registered for session '{sessionId}'.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOutcomes[sessionId] = tcs;
        return tcs.Task;
    }

    public void CompleteOutcome(string sessionId)
    {
        if (!_pendingOutcomes.Remove(sessionId, out var tcs))
        {
            throw new InvalidOperationException(
                $"No pending outcome continuation is registered for session '{sessionId}'.");
        }

        tcs.SetResult();
    }

    public void FailOutcome(string sessionId, Exception exception)
    {
        if (!_pendingOutcomes.Remove(sessionId, out var tcs))
        {
            throw new InvalidOperationException(
                $"No pending outcome continuation is registered for session '{sessionId}'.");
        }

        tcs.SetException(exception);
    }
}
