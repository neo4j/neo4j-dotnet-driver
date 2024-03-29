﻿// Copyright (c) "Neo4j"
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol;

public class TestKitProtocolException : Exception
{
    public TestKitProtocolException(string message) : base(message)
    {
    }
}

public class TestKitClientException : Exception
{
    public TestKitClientException(string message) : base(message)
    {
    }
}

public static class Protocol
{
    public static readonly HashSet<Type> ProtocolTypes = new(
        Assembly
            .GetExecutingAssembly()
            .DefinedTypes
            .Where(t => t.IsAssignableTo(typeof(ProtocolObject))));

    public static Type GetValidProtocolType(string typeName)
    {
        var type = ProtocolTypes.FirstOrDefault(t => t.Name == typeName);

        if (type == null)
        {
            throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {typeName}");
        }

        return type;
    }

    public static void ValidateType(Type objectType)
    {
        if (!ProtocolTypes.Contains(objectType))
        {
            throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {objectType}");
        }
    }
}

internal abstract class ProtocolObject
{
    public string name { get; set; }

    [JsonProperty("id")]
    public string
        uniqueId
    {
        get;
        internal set;
    } //Only exposes the get option so that the serializer will output it.  Don't want to read in on deserialization.

    [JsonIgnore] protected ProtocolObjectManager ObjManager { get; set; }

    public event EventHandler ProtocolEvent;

    public void SetObjectManager(ProtocolObjectManager objManager)
    {
        ObjManager = objManager;
    }

    public void SetUniqueId(string id)
    {
        uniqueId = id;
    }

    public virtual async Task Process()
    {
        await Task.CompletedTask;
    }

    //Default is to not use the controller object. But option to override this method and use it if necessary.
    public virtual async Task Process(Controller controller)
    {
        await Process();
    }

    public string Encode()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public virtual string Respond()
    {
        return Encode();
    }

    protected void TriggerEvent()
    {
        ProtocolEvent?.Invoke(this, EventArgs.Empty);
    }
}
