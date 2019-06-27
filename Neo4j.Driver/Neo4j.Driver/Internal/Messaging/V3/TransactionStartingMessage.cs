// Copyright (c) 2002-2019 Neo4j Sweden AB [http://neo4j.com]
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
    internal abstract class TransactionStartingMessage : IRequestMessage
    {
        private const string TxTimeoutMetadataKey = "tx_timeout";
        private const string TxMetadataMetadataKey = "tx_metadata";
        private const string AccessModeKey = "mode";

        protected TransactionStartingMessage(Bookmark bookmark, TimeSpan? txTimeout,
            IDictionary<string, object> txMetadata, AccessMode mode)
        {
            Metadata = BuildMetadata(bookmark, txTimeout ?? TimeSpan.Zero, txMetadata, mode);
        }

        public IDictionary<string, object> Metadata { get; }

        private static IDictionary<string, object> BuildMetadata(Bookmark bookmark, TimeSpan txTimeout,
            IDictionary<string, object> txMetadata, AccessMode mode)
        {
            var bookmarksPresent = bookmark?.HasBookmark ?? false;
            var txTimeoutPresent = txTimeout > TimeSpan.Zero;
            var txMetadataPresent = txMetadata != null && txMetadata.Count != 0;

            IDictionary<string, object> result = new Dictionary<string, object>();

            if (bookmarksPresent)
            {
                result.Add(Bookmark.BookmarksKey, bookmark.Bookmarks);
            }

            if (txTimeoutPresent)
            {
                var txTimeoutInMs = Math.Max(0L, (long) txTimeout.TotalMilliseconds);
                result.Add(TxTimeoutMetadataKey, txTimeoutInMs);
            }

            if (txMetadataPresent)
            {
                result.Add(TxMetadataMetadataKey, txMetadata);
            }

            switch (mode)
            {
                // We don't add a key for Write, treating it as a default
                case AccessMode.Read:
                    result.Add(AccessModeKey, "r");
                    break;
                case AccessMode.Write:
                    break;
            }

            return result;
        }
    }
}