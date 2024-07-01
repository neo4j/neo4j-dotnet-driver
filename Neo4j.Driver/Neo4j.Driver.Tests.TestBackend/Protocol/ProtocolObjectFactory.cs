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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend.Protocol;

internal static class ProtocolObjectFactory
{
    public static ProtocolObjectManager ObjManager { get; set; }

    public static ProtocolObject CreateObject(string jsonString)
    {
        var type = GetObjectType(jsonString);
        Protocol.ValidateType(type);
        return CreateObject(type, jsonString);
    }

    public static T CreateObject<T>() where T : ProtocolObject
    {
        Protocol.ValidateType(typeof(T));
        return (T)CreateObject(typeof(T));
    }

    private static ProtocolObject CreateObject(Type type, string jsonString = null)
    {
        try
        {
            var newObject = (ProtocolObject)CreateNewObjectOfType(
                type,
                jsonString,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Error
                });

            ProcessNewObject(newObject);

            return newObject;
        }
        catch (JsonException ex)
        {
            throw new Exception($"Json protocol Error: {ex.Message}");
        }
    }

    public static Type GetObjectType(string jsonString)
    {
        var objectTypeName = GetObjectTypeName(jsonString);
        return Protocol.GetValidProtocolType(objectTypeName);
    }

    private static string GetObjectTypeName(string jsonString)
    {
        var jsonObject = JObject.Parse(jsonString);
        return (string)jsonObject["name"];
    }

    private static object CreateNewObjectOfType(
        Type newType,
        string jsonString,
        JsonSerializerSettings jsonSettings = null)
    {
        var settings = jsonSettings ?? new JsonSerializerSettings();
        return string.IsNullOrEmpty(jsonString)
            ? Activator.CreateInstance(newType)
            : JsonConvert.DeserializeObject(jsonString, newType, jsonSettings);
    }

    private static T CreateNewObjectOfType<T>(string jsonString, JsonSerializerSettings jsonSettings = null)
        where T : new()
    {
        var settings = jsonSettings ?? new JsonSerializerSettings();
        return string.IsNullOrEmpty(jsonString) ? new T() : JsonConvert.DeserializeObject<T>(jsonString, settings);
    }

    private static void ProcessNewObject(ProtocolObject newObject)
    {
        newObject.SetObjectManager(ObjManager);
        ObjManager.AddProtocolObject(newObject);
    }
}
