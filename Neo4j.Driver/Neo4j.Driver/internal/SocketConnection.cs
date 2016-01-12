//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.messaging;

namespace Neo4j.Driver
{
    internal class SocketConnection : IConnection
    {
        private SocketClient _client;
        private Config config;
        private Uri url;

        public SocketConnection(Uri url, Config config)
        {
            this.url = url;
            this.config = config;
            _client = new SocketClient( url, config);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            var t = _client.Stop();
            t.Wait();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Init(string clientName)
        {
            var initMessage = new InitMessage(clientName);
            _client.Send(initMessage);
        }

        public void Sync()
        {
            throw new NotImplementedException();
        }

        public async Task<Result> Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            /*
            1. Pack statement
            2. Chunk the packed statement
            3. connection.SOMETHING(chunkedData);
            */
            //await TcpSocketClient.WriteStream.WriteAsync(0x21);
            throw new NotImplementedException();
        }
    }
}