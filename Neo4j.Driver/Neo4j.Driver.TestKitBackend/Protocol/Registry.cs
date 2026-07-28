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

namespace Neo4j.Driver.TestKitBackend.Protocol;

// One per connection scope, so handlers and the handle converters share it and handle IDs can
// never resolve across tests.
[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class Registry : IRegistry
{
    private readonly Dictionary<string, object> _objects = [];
    private int _nextId;

    public RegistryObject<T> Register<T>(T obj) where T : notnull
    {
        var id = (_nextId++).ToString();
        _objects[id] = obj;
        return new RegistryObject<T>(id, obj);
    }

    public RegistryObject<T> Get<T>(string id) where T : notnull
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

        return new RegistryObject<T>(id, typed);
    }

    public void Remove(string id)
    {
        _objects.Remove(id);
    }
}
