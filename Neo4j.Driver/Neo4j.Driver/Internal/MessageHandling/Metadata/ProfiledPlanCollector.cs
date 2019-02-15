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

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    internal class ProfiledPlanCollector : IMetadataCollector<IProfiledPlan>
    {
        private const string ProfiledPlanKey = "profile";

        public IProfiledPlan Collected { get; private set; }

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata.TryGetValue(ProfiledPlanKey, out var profiledPlanValue))
            {
                if (profiledPlanValue is IDictionary<string, object> profiledPlanDict)
                {
                    Collected = CollectProfile(profiledPlanDict);
                }
                else
                {
                    throw new ProtocolException($"Unsupported {ProfiledPlanKey} type: {profiledPlanValue.GetType().Name}");
                }
            }
        }

        private static IProfiledPlan CollectProfile(IDictionary<string, object> profileDictionary)
        {
            if (profileDictionary == null || profileDictionary.Count == 0)
            {
                return null;
            }

            var operationType = profileDictionary.GetMandatoryValue<string>("operatorType");
            var args = profileDictionary.GetValue("args", new Dictionary<string, object>());
            var identifiers = profileDictionary.GetValue("identifiers", new List<object>()).Cast<string>();
            var dbHits = profileDictionary.GetMandatoryValue<long>("dbHits");
            var rows = profileDictionary.GetMandatoryValue<long>("rows");
            var children = profileDictionary.GetValue("children", new List<object>());

            var childPlans = children
                .Select(child => child as IDictionary<string, object>)
                .Select(CollectProfile)
                .Where(childProfile => childProfile != null)
                .ToList();
            return new ProfiledPlan(operationType, args, identifiers.ToList(), childPlans, dbHits, rows);
        }
    }
}