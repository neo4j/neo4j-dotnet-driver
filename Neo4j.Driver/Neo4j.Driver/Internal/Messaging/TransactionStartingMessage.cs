// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Linq;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Messaging;

internal abstract class TransactionStartingMessage : IRequestMessage
{
    private const string BookmarksKey = "bookmarks";
    private const string TxTimeoutMetadataKey = "tx_timeout";
    private const string TxMetadataMetadataKey = "tx_metadata";
    private const string AccessModeKey = "mode";
    private const string DbKey = "db";

    protected TransactionStartingMessage(
        string database,
        Bookmarks bookmarks,
        TimeSpan? txTimeout,
        IDictionary<string, object> txMetadata,
        AccessMode mode)
    {
        Metadata = BuildMetadata(bookmarks, txTimeout, txMetadata, mode, database);
    }

    public IDictionary<string, object> Metadata { get; }

    public abstract IPackStreamSerializer Serializer { get; }

    private static IDictionary<string, object> BuildMetadata(
        Bookmarks bookmarks,
        TimeSpan? txTimeout,
        IDictionary<string, object> txMetadata,
        AccessMode mode,
        string database)
    {
        var result = new Dictionary<string, object>();

        if (bookmarks != null && bookmarks.Values.Any())
        {
            result.Add(BookmarksKey, bookmarks.Values);
        }

        if (txTimeout.HasValue)
        {
            result.Add(TxTimeoutMetadataKey, Math.Max(0L, (long)txTimeout.Value.TotalMilliseconds));
        }

        if (txMetadata != null && txMetadata.Count != 0)
        {
            result.Add(TxMetadataMetadataKey, txMetadata);
        }

        if (mode == AccessMode.Read) // We don't add a key for Write, treating it as a default
        {
            result.Add(AccessModeKey, "r");
        }

        if (!string.IsNullOrEmpty(database))
        {
            result.Add(DbKey, database);
        }

        return result;
    }
}
