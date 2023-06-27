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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class RunWithMetadataMessage : TransactionStartingMessage
{
    public RunWithMetadataMessage(
        BoltProtocolVersion version,
        Query query,
        Bookmarks bookmarks = null,
        TransactionConfig config = null,
        AccessMode mode = AccessMode.Write,
        string database = null,
        SessionConfig sessionConfig = null,
        INotificationsConfig notificationsConfig = null)
        : base(
            version,
            database,
            bookmarks,
            config?.Timeout,
            config?.Metadata,
            mode,
            notificationsConfig,
            sessionConfig)
    {
        Query = query;
    }

    public Query Query { get; }

    public override IPackStreamSerializer Serializer => RunWithMetadataMessageSerializer.Instance;

    public override string ToString()
    {
        return $"RUN {Query} {Metadata.ToContentString()}";
    }
}
