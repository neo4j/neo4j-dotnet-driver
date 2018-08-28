// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Messaging.V3
{
    internal abstract class TransactionStartingMessage : IRequestMessage
    {
        private const string TxTimeoutMetadataKey = "tx_timeout";
        private const string TxMetadataMetadataKey = "tx_metadata";

        public IDictionary<string, object> MetaData { get; }

        protected TransactionStartingMessage(Bookmark bookmark, TimeSpan txTimeout,
            IDictionary<string, object> txMetadata)
        {
            MetaData = BuildMetadata(bookmark, txTimeout, txMetadata);
        }

        protected TransactionStartingMessage(Bookmark bookmark, TransactionConfig txConfig)
        {
            MetaData = BuildMetadata(bookmark, txConfig.Timeout, txConfig.Metadata);
        }

        private static IDictionary<string, object> BuildMetadata(Bookmark bookmark, TimeSpan txTimeout,
            IDictionary<string, object> txMetadata)
        {
            var bookmarksPresent = bookmark != null && !bookmark.IsEmpty();
            var txTimeoutPresent = txTimeout > TimeSpan.Zero;
            var txMetadataPresent = txMetadata != null && txMetadata.Count != 0;

            IDictionary<string, object> result = new Dictionary<string, object>();

            if (bookmarksPresent)
            {
                result.Add(Bookmark.BookmarksKey, bookmark.Bookmarks);
            }

            if (txTimeoutPresent)
            {
                result.Add(TxTimeoutMetadataKey, txTimeout.TotalMilliseconds);
            }

            if (txMetadataPresent)
            {
                result.Add(TxMetadataMetadataKey, txMetadata);
            }

            return result;
        }
    }
}