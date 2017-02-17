// Copyright (c) 2002-2017 "Neo Technology,"
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnection : IClusterConnection
    {
        private readonly IConnection _connection;
        private readonly Action<Exception> _onErrorAction;

        public ClusterConnection(Func<IConnection> acquireConnFunc, Action<Exception> onErrorAction = null)
        {
            _onErrorAction = onErrorAction ?? (exception => { });
            try
            {
                _connection = acquireConnFunc.Invoke();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public void Sync()
        {
            try
            {
                _connection.Sync();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void Send()
        {
            try
            {
                _connection.Send();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void ReceiveOne()
        {
            try
            {
                _connection.ReceiveOne();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void Init()
        {
            try
            {
                _connection.Init();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void Run(string statement, IDictionary<string, object> parameters = null, IMessageResponseCollector resultBuilder = null,
            bool pullAll = true)
        {
            try
            {
                _connection.Run(statement, parameters, resultBuilder, pullAll);
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void Reset()
        {
            try
            {
                _connection.Reset();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public void AckFailure()
        {
            try
            {
                _connection.AckFailure();
            }
            catch (Exception e)
            {
                _onErrorAction(e);
            }
        }

        public bool IsOpen => _connection.IsOpen;
        public IServerInfo Server => _connection.Server;

        public void Close()
        {
            _connection.Close();
        }
    }
}