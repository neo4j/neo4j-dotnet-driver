// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using Neo4j.Driver.Internal.HomeDbCaching;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Messaging.ResultHandleMessage;

namespace Neo4j.Driver.Internal.MessageHandling.V4;

internal sealed class RunResponseHandler : MetadataCollectingResponseHandler
{
    private readonly HomeDbCacheKey _cacheKey;
    private readonly IHomeDbCache _homeDbCache;
    private readonly SessionConfig _sessionConfig;
    private readonly IResultStreamBuilder _streamBuilder;
    private readonly SummaryBuilder _summaryBuilder;
    private readonly bool _isDefaultDatabase;

    public RunResponseHandler(
        IResultStreamBuilder streamBuilder,
        SummaryBuilder summaryBuilder,
        HomeDbCacheKey cacheKey,
        IHomeDbCache homeDbCache,
        SessionConfig sessionConfig,
        bool isDefaultDatabase)
    {
        _streamBuilder = streamBuilder ?? throw new ArgumentNullException(nameof(streamBuilder));
        _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));
        _cacheKey = cacheKey;
        _homeDbCache = homeDbCache;
        _sessionConfig = sessionConfig;
        _isDefaultDatabase = isDefaultDatabase;

        AddMetadata<FieldsCollector, string[]>();
        AddMetadata<QueryIdCollector, long>();
        AddMetadata<TimeToFirstCollector, long>();
        AddMetadata<DatabaseInfoCollector, IDatabaseInfo>();
    }

    public override void OnSuccess(IDictionary<string, object> metadata)
    {
        base.OnSuccess(metadata);

        _summaryBuilder.ResultAvailableAfter = GetMetadata<TimeToFirstCollector, long>();

        _streamBuilder.RunCompleted(
            GetMetadata<QueryIdCollector, long>(),
            GetMetadata<FieldsCollector, string[]>(),
            null);

        var dbInfo = GetMetadata<DatabaseInfoCollector, IDatabaseInfo>();
        if (_isDefaultDatabase && _homeDbCache != null && dbInfo?.Name != null)
        {
            _sessionConfig.DriverContext.Neo4JLogger?.Debug($"Caching database name '{dbInfo.Name}' for key '{_cacheKey}'");
            _homeDbCache.AddOrUpdate(_cacheKey, dbInfo.Name);
            _sessionConfig?.PinDatabase(dbInfo.Name);
        }
    }

    public override void OnFailure(IResponsePipelineError error)
    {
        _streamBuilder.RunCompleted(NoQueryId, null, error);
    }

    public override void OnIgnored()
    {
        _streamBuilder.RunCompleted(NoQueryId, null, null);
    }
}
