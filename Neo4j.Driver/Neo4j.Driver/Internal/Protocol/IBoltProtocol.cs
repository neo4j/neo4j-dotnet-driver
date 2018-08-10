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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Protocol
{
    internal interface IBoltProtocol
    {
        IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null, bool byteArraySupportEnabled = true);
        IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null, bool byteArraySupportEnabled = true);

        void InitializeConnection(IConnection connection, string userAgent, IAuthToken authToken);
        Task InitializeConnectionAsync(IConnection connection, string userAgent, IAuthToken authToken);

        IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler);

        Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler);

        void BeginTransaction(IConnection connection, Bookmark bookmark);
        Task BeginTransactionAsync(IConnection connection, Bookmark bookmark);

        IStatementResult RunInExplicitTransaction(IConnection connection, Statement statement);
        Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection, Statement statement);

        Bookmark CommitTransaction(IConnection connection);
        Task<Bookmark> CommitTransactionAsync(IConnection connection);

        void RollbackTransaction(IConnection connection);
        Task RollbackTransactionAsync(IConnection connection);

        void Reset(IConnection connection);
    }
}