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

using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Session;

internal class RetryablePositive : ProtocolObject
{
    public RetryablePositiveType data { get; set; } = new();

    public override async Task Process()
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
        sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAblePositive);

        //Client succeded and wants to commit. 
        //Notify any subscribers.

        TriggerEvent();

        await Task.CompletedTask;
    }

    public override string Respond()
    {
        return string.Empty;
    }

    public class RetryablePositiveType
    {
        public string sessionId { get; set; }
    }
}
