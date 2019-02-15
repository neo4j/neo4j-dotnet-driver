// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.MessageHandling
{
    internal abstract class MetadataCollectingResponseHandler : IResponseHandler
    {
        private readonly IDictionary<Type, IMetadataCollector> _metadataCollectors;

        protected MetadataCollectingResponseHandler()
        {
            _metadataCollectors = new Dictionary<Type, IMetadataCollector>();
        }

        protected void AddMetadata<TCollector, TMetadata>()
            where TCollector : class, IMetadataCollector<TMetadata>, new()
        {
            var collectorType = typeof(TCollector);
            if (_metadataCollectors.ContainsKey(collectorType))
            {
                throw new InvalidOperationException(
                    $"A metadata collector of type {typeof(TCollector).Name} is already registered.");
            }

            _metadataCollectors.Add(collectorType, new TCollector());
        }

        protected TCollector RemoveMetadata<TCollector, TMetadata>()
            where TCollector : class, IMetadataCollector<TMetadata>, new()
        {
            var collectorType = typeof(TCollector);
            if (!_metadataCollectors.TryGetValue(collectorType, out var collector)) return null;
            _metadataCollectors.Remove(collectorType);
            return (TCollector) collector;
        }

        protected TMetadata GetMetadata<TCollector, TMetadata>()
            where TCollector : class, IMetadataCollector<TMetadata>, new()
        {
            return _metadataCollectors.TryGetValue(typeof(TCollector), out var collector)
                ? ((IMetadataCollector<TMetadata>) collector).Collected
                : default(TMetadata);
        }

        public virtual Task OnSuccessAsync(IDictionary<string, object> metadata)
        {
            foreach (var collector in _metadataCollectors.Values)
            {
                collector.Collect(metadata);
            }

            return TaskHelper.GetCompletedTask();
        }

        public virtual Task OnRecordAsync(object[] fieldValues)
        {
            return TaskHelper.GetFailedTask(
                new ProtocolException($"{nameof(OnRecordAsync)} is not expected at this time."));
        }

        public virtual Task OnFailureAsync(IResponsePipelineError error)
        {
            return TaskHelper.GetCompletedTask();
        }

        public virtual Task OnIgnoredAsync()
        {
            return TaskHelper.GetCompletedTask();
        }
    }
}