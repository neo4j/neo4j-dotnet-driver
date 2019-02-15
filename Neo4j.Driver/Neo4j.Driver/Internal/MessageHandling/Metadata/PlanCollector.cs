﻿// Copyright (c) 2002-2019 "Neo4j,"
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
    internal class PlanCollector : IMetadataCollector<IPlan>
    {
        private const string PlanKey = "plan";

        public IPlan Collected { get; private set; }

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata.TryGetValue(PlanKey, out var planValue))
            {
                if (planValue is IDictionary<string, object> planDict)
                {
                    Collected = CollectPlan(planDict);
                }
                else
                {
                    throw new ProtocolException($"Unsupported {PlanKey} type: {planValue.GetType().Name}");
                }
            }
        }

        private static IPlan CollectPlan(IDictionary<string, object> planDictionary)
        {
            if (planDictionary == null || planDictionary.Count == 0)
            {
                return null;
            }

            var operationType = planDictionary.GetMandatoryValue<string>("operatorType");
            var args = planDictionary.GetValue("args", new Dictionary<string, object>());
            var identifiers = planDictionary.GetValue("identifiers", new List<object>()).Cast<string>();
            var children = planDictionary.GetValue("children", new List<object>());

            var childPlans = children
                .Select(child => child as IDictionary<string, object>)
                .Select(CollectPlan)
                .Where(childPlan => childPlan != null)
                .ToList();

            return new Plan(operationType, args, identifiers.ToList(), childPlans);
        }
    }
}