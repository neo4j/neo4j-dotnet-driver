// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Tests.TestBackend;

internal class NewDriverConverter : JsonConverter<NewDriver.NewDriverType>
{
    public override void WriteJson(JsonWriter writer, NewDriver.NewDriverType value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override NewDriver.NewDriverType ReadJson(
        JsonReader reader,
        Type objectType,
        NewDriver.NewDriverType existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jsonObj = JObject.Load(reader);

        var newDriverRequest = new NewDriver.NewDriverType();
        newDriverRequest.authorizationToken = jsonObj["authorizationToken"]?.ToObject<AuthorizationToken>();
        newDriverRequest.authTokenManagerId = jsonObj["authTokenManagerId"]?.Value<string>();
        newDriverRequest.uri = jsonObj["uri"]?.Value<string>();
        newDriverRequest.userAgent = jsonObj["userAgent"]?.Value<string>();
        newDriverRequest.resolverRegistered = jsonObj["resolverRegistered"]?.Value<bool>() ?? false;
        newDriverRequest.domainNameResolverRegistered = jsonObj["domainNameResolverRegistered"]?.Value<bool>() ?? false;
        newDriverRequest.connectionTimeoutMs = jsonObj["connectionTimeoutMs"]?.Value<int?>() ?? -1;
        newDriverRequest.maxConnectionPoolSize = jsonObj["maxConnectionPoolSize"]?.Value<int?>();
        newDriverRequest.connectionAcquisitionTimeoutMs = jsonObj["connectionAcquisitionTimeoutMs"]?.Value<int?>();
        newDriverRequest.fetchSize = jsonObj["fetchSize"]?.Value<long?>();
        newDriverRequest.maxTxRetryTimeMs = jsonObj["maxTxRetryTimeMs"]?.Value<long?>();

        if (jsonObj.TryGetValue("trustedCertificates", out var token))
        {
            newDriverRequest.trustedCertificates = token.ToObject<string[]>();
        }

        if (jsonObj.TryGetValue("encrypted", out token))
        {
            newDriverRequest.encrypted = token.Value<bool?>();
        }

        return newDriverRequest;
    }
}
