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
using System.Linq;

namespace Neo4j.Driver.Internal.Messaging.V3
{
    internal class HelloMessage : IRequestMessage
    {
        private const string UserAgentMetadataKey = "user_agent";
            
        public HelloMessage(string userAgent, IDictionary<string, object> authToken)
        {
            if (authToken == null || authToken.Count == 0)
            {
                MetaData = new Dictionary<string, object>{{UserAgentMetadataKey, userAgent}};
            }
            else
            {
                MetaData = new Dictionary<string, object>(authToken) {{UserAgentMetadataKey, userAgent}};
            }
        }

        public IDictionary<string, object> MetaData { get; }

        public override string ToString()
        {
            IDictionary<string, object> metadataCopy = new Dictionary<string, object>(MetaData);
            if (metadataCopy.ContainsKey(AuthToken.CredentialsKey))
            {
                metadataCopy[AuthToken.CredentialsKey] = "******";
            }
            return "HELLO " + metadataCopy.ToContentString();
        }
    }
}