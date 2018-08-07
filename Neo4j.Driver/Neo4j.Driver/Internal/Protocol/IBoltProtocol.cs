// Copyright (c) 2002-2018 "Neo4j,"
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

using System.IO;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Protocol
{
    internal interface IBoltProtocol
    {
        IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger=null);
        IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger=null);
        
        void BeginTransaction(IConnection connection, IConnectionContext context);
        Bookmark CommitTransaction(IConnection connection, IConnectionContext context);
        void RollbackTransaction(IConnection connection, IConnectionContext context);
        IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement, IResultResourceHandler resultResourceHandler);
        IStatementResult RunInExplicitTransaction(IConnection connection, IConnectionContext context);
        void InitializeConnection(IConnection connection, string userAgent, IAuthToken authToken);
        
    }

    internal interface IConnectionContext
    {
    }
}
