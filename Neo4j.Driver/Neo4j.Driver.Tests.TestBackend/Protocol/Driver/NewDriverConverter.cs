// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewDriverConverter: JsonConverter<NewDriver.NewDriverType>
    {
        public override void WriteJson(JsonWriter writer, NewDriver.NewDriverType value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override NewDriver.NewDriverType ReadJson(JsonReader reader, Type objectType, NewDriver.NewDriverType existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jsonObj = JObject.Load(reader);

            var type = new NewDriver.NewDriverType();
            type.authorizationToken = jsonObj[nameof(NewDriver.NewDriverType.authorizationToken)]
                ?.ToObject<AuthorizationToken>();
            type.uri = jsonObj[nameof(NewDriver.NewDriverType.uri)]
                ?.Value<string>();
            type.userAgent = jsonObj[nameof(NewDriver.NewDriverType.userAgent)]
                ?.Value<string>();
            type.resolverRegistered = jsonObj[nameof(NewDriver.NewDriverType.resolverRegistered)]
                ?.Value<bool>() ?? false;
            type.domainNameResolverRegistered = jsonObj[nameof(NewDriver.NewDriverType.domainNameResolverRegistered)]
                ?.Value<bool>() ?? false;
            type.connectionTimeoutMs = jsonObj[nameof(NewDriver.NewDriverType.connectionTimeoutMs)]
                ?.Value<int?>() ?? -1;
            type.maxConnectionPoolSize = jsonObj[nameof(NewDriver.NewDriverType.maxConnectionPoolSize)]
                ?.Value<int?>();
            type.connectionAcquisitionTimeoutMs = jsonObj[nameof(NewDriver.NewDriverType.connectionAcquisitionTimeoutMs)]
                ?.Value<int?>();
            type.fetchSize = jsonObj[nameof(NewDriver.NewDriverType.fetchSize)]
                ?.Value<int?>();
            
            if (jsonObj.TryGetValue(nameof(NewDriver.NewDriverType.trustedCertificates), out var token))
                type.trustedCertificates = token.ToObject<string[]>();

            if (jsonObj.TryGetValue(nameof(NewDriver.NewDriverType.encrypted), out token))
                type.encrypted = token.Value<bool?>();

            return type;
        }
    }
}