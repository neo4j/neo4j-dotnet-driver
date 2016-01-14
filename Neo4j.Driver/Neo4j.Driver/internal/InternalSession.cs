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
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    public class InternalSession : ISession
    {
        private readonly IConnection _connection;

        public InternalSession(Uri url, Config config)
        {
            _connection = new SocketConnection(url, config);
        }

        public void Dispose()
        {
//            throw new NotImplementedException();
        }

        public Result Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            var resultBuilder = new ResultBuilder();
            _connection.Run(resultBuilder, statement, statementParameters);
            _connection.PullAll(resultBuilder);
            _connection.Sync();

            return resultBuilder.Build();
        }
    }
}