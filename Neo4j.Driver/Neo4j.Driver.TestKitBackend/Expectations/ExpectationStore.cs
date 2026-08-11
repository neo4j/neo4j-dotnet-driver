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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Expectations;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class ExpectationStore : IExpectationStore, IDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, TaskCompletionSource<object?>> _pending = [];
    private readonly ILogger _logger;

    public ExpectationStore(ILogger logger)
    {
        _logger = logger;
    }

    public Task<T> Expect<T>(string key)
    {
        lock (_lock)
        {
            if (_pending.ContainsKey(key))
            {
                throw new TestKitProtocolException($"An expectation is already pending for key '{key}'.");
            }

            var source = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[key] = source;
            return CastAsync<T>(key, source.Task);
        }
    }

    public void Fulfil<T>(string key, T value)
    {
        Remove(key).SetResult(value);
    }

    public void Fail(string key, Exception error)
    {
        Remove(key).SetException(error);
    }

    private TaskCompletionSource<object?> Remove(string key)
    {
        lock (_lock)
        {
            return _pending.Remove(key, out var source)
                ? source
                : throw new TestKitProtocolException($"No expectation is pending for key '{key}'.");
        }
    }

    public void Dispose()
    {
        List<KeyValuePair<string, TaskCompletionSource<object?>>> outstanding;
        lock (_lock)
        {
            outstanding = [.._pending];
            _pending.Clear();
        }

        foreach (var (key, source) in outstanding)
        {
            _logger.LogWarning(
                "An expectation for key '{Key}' was still outstanding when the connection closed",
                key);

            source.TrySetCanceled();
        }
    }

    private static async Task<T> CastAsync<T>(string key, Task<object?> task)
    {
        var value = await task;
        var expected = typeof(T).Name;
        var found = value?.GetType().Name ?? "null";

        return value is T typed
            ? typed
            : throw new TestKitProtocolException(
                $"Expectation for key '{key}' required {expected} but was fulfilled with {found}.");
    }
}
