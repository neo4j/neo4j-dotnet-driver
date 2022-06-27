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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface IConnection
    {
        Task InitAsync(CancellationToken cancellationToken = default);

        // send all and receive all
        Task SyncAsync();

        // send all
        Task SendAsync();

        // receive one
        Task ReceiveOneAsync();

        Task EnqueueAsync(IRequestMessage message1, IResponseHandler handler1, IRequestMessage message2 = null,
            IResponseHandler handler2 = null);

        // Enqueue a reset message
        Task ResetAsync();

        /// <summary>
        /// Close and release related resources
        /// </summary>
        Task DestroyAsync();

        /// <summary>
        /// Close connection
        /// </summary>
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
        /// The Database this connection is acquired for.
        /// </summary>
        string Database { get; set; }

        void UpdateId(string newConnId);

        void UpdateVersion(ServerVersion newVersion);

        IDictionary<string, string> RoutingContext { get; set; }

		void SetRecvTimeOut(int seconds);

        void SetUseUtcEncodedDateTime();
    }
}