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

namespace Neo4j.Driver.TestKitBackend.ObjectStorage;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class ObjectStore : IObjectStore, IAsyncDisposable
{
    private readonly OrderedDictionary<string, object> _objects = [];
    private readonly ILogger _logger;
    private int _nextId;

    public ObjectStore(ILogger logger)
    {
        _logger = logger;
    }

    public Stored<T> Register<T>(T obj) where T : notnull
    {
        return Register(_ => obj);
    }

    public Stored<T> Register<T>(Func<string, T> create) where T : notnull
    {
        var id = (_nextId++).ToString();
        var obj = create(id);
        _objects[id] = obj;
        return new Stored<T>(id, obj);
    }

    public Stored<T> Get<T>(string id) where T : notnull
    {
        if (!_objects.TryGetValue(id, out var obj))
        {
            throw new TestKitProtocolException($"No object is registered with id '{id}'.");
        }

        if (obj is not T typed)
        {
            throw new TestKitProtocolException(
                $"The object registered with id '{id}' is a {obj.GetType().Name}, not a {typeof(T).Name}.");
        }

        return new Stored<T>(id, typed);
    }

    public void Remove(string id)
    {
        _objects.Remove(id);
    }

    public async ValueTask DisposeAsync()
    {
        for (var i = _objects.Count - 1; i >= 0; i--)
        {
            var obj = _objects.GetAt(i).Value;
            try
            {
                if (obj is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to dispose a registered {Type}", obj.GetType().Name);
            }
        }

        _objects.Clear();
    }
}
