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

namespace Neo4j.Driver.Internal.MessageHandling;

internal abstract class MetadataCollectingResponseHandler : IResponseHandler
{
    private readonly IDictionary<Type, IMetadataCollector> _metadataCollectors;

    protected MetadataCollectingResponseHandler()
    {
        _metadataCollectors = new Dictionary<Type, IMetadataCollector>();
    }

    public virtual void OnSuccess(IDictionary<string, object> metadata)
    {
        foreach (var collector in _metadataCollectors.Values)
        {
            collector.Collect(metadata);
        }
    }

    public virtual void OnRecord(object[] fieldValues)
    {
        throw new ProtocolException($"{nameof(OnRecord)} is not expected at this time.");
    }

    public virtual void OnFailure(IResponsePipelineError error)
    {
    }

    public virtual void OnIgnored()
    {
    }

    protected void AddMetadata<TCollector, TMetadata>()
        where TCollector : class, IMetadataCollector<TMetadata>, new()
    {
        AddMetadata<TCollector, TMetadata>(new TCollector());
    }

    protected void AddMetadata<TCollector, TMetadata>(TCollector collector)
        where TCollector : class, IMetadataCollector<TMetadata>
    {
        var collectorType = typeof(TCollector);
        if (_metadataCollectors.ContainsKey(collectorType))
        {
            throw new InvalidOperationException(
                $"A metadata collector of type {typeof(TCollector).Name} is already registered.");
        }

        _metadataCollectors.Add(collectorType, collector ?? throw new ArgumentNullException(nameof(collector)));
    }

    protected TMetadata GetMetadata<TCollector, TMetadata>()
        where TCollector : class, IMetadataCollector<TMetadata>
    {
        if (_metadataCollectors.TryGetValue(typeof(TCollector), out var collector)) {
            return ((IMetadataCollector<TMetadata>)collector).Collected;
        }

        return default;
    }
}
