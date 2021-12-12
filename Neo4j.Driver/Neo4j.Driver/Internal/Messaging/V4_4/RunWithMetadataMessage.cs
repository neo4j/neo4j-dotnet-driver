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
using Neo4j.Driver;
using Neo4j.Driver.Internal.Messaging.V3;

namespace Neo4j.Driver.Internal.Messaging.V4_4
{
	internal class RunWithMetadataMessage : TransactionStartingMessage
	{
		public RunWithMetadataMessage(Query query, AccessMode mode)
			: this(query, null, null, mode, null)
		{
		}

		public RunWithMetadataMessage(Query query, Bookmark bookmark = null, TransactionConfig configBuilder = null,
			AccessMode mode = AccessMode.Write, string impersonatedUser = null)
			: this(query, null, bookmark, configBuilder?.Timeout, configBuilder?.Metadata, mode, impersonatedUser)
		{
		}

		public RunWithMetadataMessage(Query query, string database, Bookmark bookmark, TransactionConfig configBuilder,
			AccessMode mode, string impersonatedUser)
			: this(query, database, bookmark, configBuilder?.Timeout, configBuilder?.Metadata, mode, impersonatedUser)
		{
		}
		
		public RunWithMetadataMessage(Query query, string database, Bookmark bookmark, TimeSpan? txTimeout,
			IDictionary<string, object> txMetadata, AccessMode mode, string impersonatedUser)
			: base(database, bookmark, txTimeout, txMetadata, mode)
		{
			Query = query;

			if (!string.IsNullOrEmpty(impersonatedUser))
			{
				Metadata.Add("imp_user", impersonatedUser);
			}
		}

		public Query Query { get; }

		public override string ToString()
		{
			return $"RUN {Query} {Metadata.ToContentString()}";
		}
	}
}