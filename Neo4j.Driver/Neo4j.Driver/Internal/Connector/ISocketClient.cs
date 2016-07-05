// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface ISocketClient
    {
        Task Start();
        Task Stop();
        void Send(IEnumerable<IRequestMessage> messages);
        /* Recieve until unhandledMessageSize messages are left in unhandled message queue */
        void Receive(IMessageResponseHandler responseHandler, int unhandledMessageSize = 0);
        /* Return true if a record message is received, otherwise false. */
        bool ReceiveOneRecordMessage(IMessageResponseHandler responseHandler, Action onFailureAction );
        bool IsOpen { get; }
    }
}