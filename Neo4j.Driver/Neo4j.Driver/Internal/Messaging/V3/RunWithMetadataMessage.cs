// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Messaging.V3
{
    internal class RunWithMetadataMessage : TransactionStartingMessage
    {
        public RunWithMetadataMessage(Statement statement, AccessMode mode)
            : this(statement, null, null, mode)
        {
        }

        public RunWithMetadataMessage(Statement statement, Bookmark bookmark = null, TransactionOptions optionsBuilder = null,
            AccessMode mode = AccessMode.Write)
            : this(statement, null, bookmark, optionsBuilder?.Timeout, optionsBuilder?.Metadata, mode)
        {
        }

        public RunWithMetadataMessage(Statement statement, string database, Bookmark bookmark, TransactionOptions optionsBuilder,
            AccessMode mode)
            : this(statement, database, bookmark, optionsBuilder?.Timeout, optionsBuilder?.Metadata, mode)
        {
        }

        public RunWithMetadataMessage(Statement statement, string database, Bookmark bookmark, TimeSpan? txTimeout,
            IDictionary<string, object> txMetadata, AccessMode mode)
            : base(database, bookmark, txTimeout, txMetadata, mode)
        {
            Statement = statement;
        }

        public Statement Statement { get; }

        public override string ToString()
        {
            return $"RUN {Statement} {Metadata.ToContentString()}";
        }
    }
}