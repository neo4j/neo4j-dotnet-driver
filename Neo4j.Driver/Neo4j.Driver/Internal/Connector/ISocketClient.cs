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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface ISocketClient
    {
        Task<IBoltProtocol> ConnectAsync(IDictionary<string, string> routingContext, CancellationToken token = default);
        Task SendAsync(IEnumerable<IRequestMessage> messages);
        Task ReceiveAsync(IResponsePipeline responsePipeline);
        Task ReceiveOneAsync(IResponsePipeline responsePipeline);
        bool IsOpen { get; }
        Task StopAsync();
		void SetRecvTimeOut(int seconds);
	}
}