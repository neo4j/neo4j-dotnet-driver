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
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Messaging.ResultHandleMessage;

namespace Neo4j.Driver.Internal.MessageHandling.V4;

internal sealed class RunResponseHandler : MetadataCollectingResponseHandler
{
    private readonly IResultStreamBuilder _streamBuilder;
    private readonly SummaryBuilder _summaryBuilder;

    public RunResponseHandler(IResultStreamBuilder streamBuilder, SummaryBuilder summaryBuilder)
    {
        _streamBuilder = streamBuilder ?? throw new ArgumentNullException(nameof(streamBuilder));
        _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));

        AddMetadata<FieldsCollector, string[]>();
        AddMetadata<QueryIdCollector, long>();
        AddMetadata<TimeToFirstCollector, long>();
    }

    public override void OnSuccess(IDictionary<string, object> metadata)
    {
        base.OnSuccess(metadata);

        _summaryBuilder.ResultAvailableAfter = GetMetadata<TimeToFirstCollector, long>();

        _streamBuilder.RunCompleted(
            GetMetadata<QueryIdCollector, long>(),
            GetMetadata<FieldsCollector, string[]>(),
            null);
    }

    public override void OnFailure(IResponsePipelineError error)
    {
        _streamBuilder.RunCompleted(NoQueryId, null, error);
        error.EnsureThrown();
    }

    public override void OnIgnored()
    {
        _streamBuilder.RunCompleted(NoQueryId, null, null);
    }
}
