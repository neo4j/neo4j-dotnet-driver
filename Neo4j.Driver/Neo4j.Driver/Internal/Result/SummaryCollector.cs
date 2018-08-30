// Copyright (c) 2002-2018 "Neo4j,"
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
using static Neo4j.Driver.V1.StatementType;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class SummaryCollector
    {
        private readonly SummaryBuilder _summaryBuilder;
        public SummaryBuilder SummaryBuilder => _summaryBuilder;

        public SummaryCollector(Statement statement, IServerInfo server)
        {
            _summaryBuilder = new SummaryBuilder(statement, server);
        }

        public void CollectWithFields(IDictionary<string, object> meta)
        {
            CollectResultAvailableAfter(meta);
        }

        public void Collect(IDictionary<string, object> meta)
        {
            CollectType(meta, "type");
            CollectCounters(meta, "stats");
            CollectPlan(meta, "plan");
            CollectProfile(meta, "profile");
            CollectNotifications(meta, "notifications");
            CollectResultConsumedAfter(meta);
        }

        public IResultSummary Build()
        {
            return _summaryBuilder.Build();
        }

        private void CollectType(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }
            var type = meta[name] as string;
            _summaryBuilder.StatementType = FromCode(type);
        }

        private void CollectCounters(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }
            var stats = meta[name] as IDictionary<string, object>;

            _summaryBuilder.Counters = new Counters(
                CountersValue(stats, "nodes-created"),
                CountersValue(stats, "nodes-deleted"),
                CountersValue(stats, "relationships-created"),
                CountersValue(stats, "relationships-deleted"),
                CountersValue(stats, "properties-set"),
                CountersValue(stats, "labels-added"),
                CountersValue(stats, "labels-removed"),
                CountersValue(stats, "indexes-added"),
                CountersValue(stats, "indexes-removed"),
                CountersValue(stats, "constraints-added"),
                CountersValue(stats, "constraints-removed"));
        }

        private void CollectPlan(IDictionary<string, object> meta, string name)
        {
            if (meta == null || !meta.ContainsKey(name))
            {
                return;
            }
            var planDictionary = meta[name] as IDictionary<string, object>;
            _summaryBuilder.Plan = CollectPlan(planDictionary);
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

        private void CollectProfile(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }
            var profiledPlan = meta[name] as IDictionary<string, object>;
            _summaryBuilder.Profile = CollectProfile(profiledPlan);
        }

        private void CollectNotifications(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }
            var list = (meta[name] as List<object>).Cast<IDictionary<string, object>>();
            var notifications = new List<INotification>();
            foreach (var value in list)
            {
                var code = value.GetValue("code", string.Empty);
                var title = value.GetValue("title", string.Empty);
                var description = value.GetValue("description", string.Empty);

                var posValue = value.GetValue("position", new Dictionary<string, object>());

                var position = new InputPosition(
                    (int)posValue.GetValue("offset", 0L),
                    (int)posValue.GetValue("line", 0L),
                    (int)posValue.GetValue("column", 0L));
                var severity = value.GetValue("severity", string.Empty);
                notifications.Add(new Notification(code, title, description, position, severity));
            }
            _summaryBuilder.Notifications = notifications;
        }

        protected virtual void CollectResultAvailableAfter(IDictionary<string, object> meta)
        {
            var name = "result_available_after";
            if (!meta.ContainsKey(name))
            {
                return;
            }
            _summaryBuilder.ResultAvailableAfter = meta[name].As<long>();
        }

        protected virtual void CollectResultConsumedAfter(IDictionary<string, object> meta)
        {
            var name = "result_consumed_after";
            if (!meta.ContainsKey(name))
            {
                return;
            }
            _summaryBuilder.ResultConsumedAfter = meta[name].As<long>();
        }

        private static int CountersValue(IDictionary<string, object> counters, string name)
        {
            return (int)counters.GetValue(name, 0L);
        }

        private static StatementType FromCode(string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "r":
                    return ReadOnly;
                case "rw":
                    return ReadWrite;
                case "w":
                    return WriteOnly;
                case "s":
                    return SchemaWrite;
                default:
                    throw new ClientException("Unknown statement type: `" + type + "`.");
            }
        }
    }
}
