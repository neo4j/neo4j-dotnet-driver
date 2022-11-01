// Copyright (c) "Neo4j"
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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Messaging.V5_1;

internal class HelloMessage : IRequestMessage
{
    public Dictionary<string, object> MetaData { get; }
    public Dictionary<string, object> Auth { get; }

    private const string UserAgentMetadataKey = "user_agent";

    public HelloMessage(string userAgent, IDictionary<string, object> authToken) :
        this(userAgent, authToken, null, null)
    {
    }

    public HelloMessage(string userAgent, IDictionary<string, object> authToken, IDictionary<string, string> routingContext, string[] notificationFilters)
    {
        Auth = authToken != null
            ? new Dictionary<string, object>(authToken) 
            : new Dictionary<string, object>();

        MetaData = new Dictionary<string, object>
        {
            [UserAgentMetadataKey] = userAgent,
            ["routing"] = routingContext
        };

        if (notificationFilters is not null)
            MetaData.Add("notifications", notificationFilters);
    }

    public override string ToString()
    {
        var authDictionaryClone = new Dictionary<string, object>(Auth);

        if (authDictionaryClone.ContainsKey(AuthToken.CredentialsKey))
            authDictionaryClone[AuthToken.CredentialsKey] = "******";

        return $"HELLO {authDictionaryClone.ToContentString()} {MetaData.ToContentString()}";
    }
}