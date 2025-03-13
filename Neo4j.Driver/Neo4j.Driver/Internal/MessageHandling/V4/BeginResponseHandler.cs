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

using System.Collections.Generic;
using Neo4j.Driver.Internal.HomeDbCaching;
using Neo4j.Driver.Internal.MessageHandling.Metadata;

namespace Neo4j.Driver.Internal.MessageHandling.V4;

internal sealed class BeginResponseHandler : MetadataCollectingResponseHandler
{
    private readonly HomeDbCacheKey _cacheKey;
    private readonly IHomeDbCache _homeDbCache;
    private readonly SessionConfig _sessionConfig;
    private readonly bool _isDefaultDatabase;

    public BeginResponseHandler(
        HomeDbCacheKey cacheKey,
        IHomeDbCache homeDbCache,
        SessionConfig sessionConfig,
        bool isDefaultDatabase)
    {
        _cacheKey = cacheKey;
        _homeDbCache = homeDbCache;
        _sessionConfig = sessionConfig;
        _isDefaultDatabase = isDefaultDatabase;
        AddMetadata<DatabaseInfoCollector, IDatabaseInfo>();
    }

    public override void OnSuccess(IDictionary<string, object> metadata)
    {
        base.OnSuccess(metadata);

        var dbInfo = GetMetadata<DatabaseInfoCollector, IDatabaseInfo>();
        if (_isDefaultDatabase && _homeDbCache != null && dbInfo?.Name != null)
        {
            _sessionConfig.DriverContext.Logger?.Debug(
                $"Caching database name '{dbInfo.Name}' for key '{_cacheKey}'");

            _homeDbCache.AddOrUpdate(_cacheKey, dbInfo.Name);
            _sessionConfig?.PinDatabase(dbInfo.Name);
        }
    }
}
