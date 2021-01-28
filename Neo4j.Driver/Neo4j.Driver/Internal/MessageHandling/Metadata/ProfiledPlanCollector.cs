﻿// Copyright (c) "Neo4j"
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

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    internal class ProfiledPlanCollector : IMetadataCollector<IProfiledPlan>
    {
        internal const string ProfiledPlanKey = "profile";

        object IMetadataCollector.Collected => Collected;

        public IProfiledPlan Collected { get; private set; }

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata != null && metadata.TryGetValue(ProfiledPlanKey, out var profiledPlanValue))
            {
                switch (profiledPlanValue)
                {
                    case null:
                        Collected = null;
                        break;
                    case IDictionary<string, object> profiledPlanDict:
                        Collected = CollectProfile(profiledPlanDict);
                        break;
                    default:
                        throw new ProtocolException(
                            $"Expected '{ProfiledPlanKey}' metadata to be of type 'IDictionary<String,Object>', but got '{profiledPlanValue?.GetType().Name}'.");
                }
            }
        }

        private static IProfiledPlan CollectProfile(IDictionary<string, object> profileDictionary)
        {
            if (profileDictionary.Count == 0)
            {
                return null;
            }

            var operationType =
                profileDictionary.GetMandatoryValue<string>("operatorType", m => new ProtocolException(m));
            var args = profileDictionary.GetValue("args", new Dictionary<string, object>());
            var identifiers = profileDictionary.GetValue("identifiers", new List<object>()).Cast<string>();
            var dbHits = profileDictionary.GetMandatoryValue<long>("dbHits", m => new ProtocolException(m));
            var rows = profileDictionary.GetMandatoryValue<long>("rows", m => new ProtocolException(m));
            var pageCacheHits = profileDictionary.GetValue<long>("pageCacheHits", 0);
            var pageCacheMisses = profileDictionary.GetValue<long>("pageCacheMisses", 0);
            var pageCacheHitRatio = profileDictionary.GetValue<double>("pageCacheHitRatio", 0);
            var time = profileDictionary.GetValue<long>("time", 0);
            var children = profileDictionary.GetValue("children", new List<object>());

            var childPlans = children
                .Select(child => child as IDictionary<string, object>)
                .Select(CollectProfile)
                .Where(childProfile => childProfile != null)
                .ToList();
            return new ProfiledPlan(operationType, args, identifiers.ToList(), childPlans, dbHits, rows,
                pageCacheHits, pageCacheMisses, pageCacheHitRatio, time);
        }
    }
}