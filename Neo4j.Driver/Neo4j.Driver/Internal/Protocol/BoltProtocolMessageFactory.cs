﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal;

internal interface IBoltProtocolMessageFactory
{
    IRequestMessage NewRunWithMetadataMessage(SummaryBuilder summaryBuilder, IConnection connection, AutoCommitParams autoCommitParams);
}
internal class BoltProtocolMessageFactory : IBoltProtocolMessageFactory
{
    public IRequestMessage NewRunWithMetadataMessage(SummaryBuilder summaryBuilder, IConnection connection, AutoCommitParams autoCommitParams)
    {
        return // Refactor to take AC Params
            new RunWithMetadataMessage(
            connection.Version,
            autoCommitParams.Query,
            autoCommitParams.Bookmarks,
            autoCommitParams.Config,
            connection.Mode ?? throw new InvalidOperationException("Connection should have its Mode property set."),
            autoCommitParams.Database,
            autoCommitParams.ImpersonatedUser);
    }
}