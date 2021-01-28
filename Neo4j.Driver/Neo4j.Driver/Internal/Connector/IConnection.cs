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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface IConnection
    {
        void Init();

        Task InitAsync();

        // send all and receive all
        void Sync();

        Task SyncAsync();

        // send all
        void Send();

        Task SendAsync();

        // receive one
        void ReceiveOne();

        Task ReceiveOneAsync();

        void Enqueue(IRequestMessage message1, IMessageResponseCollector responseCollector,
            IRequestMessage message2 = null);

        // Enqueue a reset message
        void Reset();

        /// <summary>
        /// Close and release related resources
        /// </summary>
        void Destroy();

        /// <summary>
        /// Close and release related resources
        /// </summary>
        Task DestroyAsync();

        /// <summary>
        /// Close connection
        /// </summary>
        void Close();

        Task CloseAsync();

        /// <summary>
        /// Return true if the underlying socket connection is till open, otherwise false.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// The info of the server the connection connected to.
        /// </summary>
        IServerInfo Server { get; }

        /// <summary>
        /// The Bolt protocol that the connection is talking with.
        /// </summary>
        IBoltProtocol BoltProtocol { get; }

        /// <summary>
        /// The AccessMode this connection is operating in.
        /// </summary>
        AccessMode? Mode { get; set; }

        /// <summary>
        /// Downgrade message reader and writer to not be able to read and write byte array
        /// </summary>
        void ResetMessageReaderAndWriterForServerV3_1();

        void UpdateId(string newConnId);
    }
}