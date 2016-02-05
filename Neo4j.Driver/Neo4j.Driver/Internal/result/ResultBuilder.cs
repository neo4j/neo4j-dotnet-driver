//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Extensions;
using static Neo4j.Driver.StatementType;

namespace Neo4j.Driver.Internal.result
{
    public class ResultBuilder
    {
        //private IDictionary<string, dynamic> _meta;
        private string[] _keys = new string[0];
        private readonly IList<Record> _records = new List<Record>();
        private readonly SummaryBuilder _summaryBuilder;
        private bool _noMoreRecords = false;

        internal ResultBuilder() : this(null, null)
        {
        }

        public ResultBuilder(string statement, IDictionary<string, object> parameters)
        {
            _summaryBuilder = new SummaryBuilder(new Statement(statement, parameters));
        }

        public void Record(dynamic[] fields)
        {
            var record = new Record(_keys, fields);
            _records.Add(record);
        }

        public void NoMoreRecords()
        {
            _noMoreRecords = true;
        }

        // do not change this code!!!
        private IEnumerable<Record> RecordsStream()
        {
            int index = 0;

            while (!_noMoreRecords || index <= _records.Count)
            {
                while (index == _records.Count)
                {
                    Task.Delay(50).Wait();
                    if (_noMoreRecords && index == _records.Count)
                        yield break;
                }

                yield return _records[index];
                index++;
            }
        } 

        public ResultCursor Build()
        {
            return new ResultCursor(_keys, RecordsStream(), () => _summaryBuilder.Build()); // TODO
        }

        public void CollectFields(IDictionary<string, object> meta)
        {
            if (meta == null)
            {
                return;
            }
            CollectKeys(meta, "fields");
        }

        public void CollectSummaryMeta(IDictionary<string, object> meta)
        {
            NoMoreRecords();
            if (meta == null)
            {
                return;
            }

            CollectType(meta, "type");
            CollectStatistics(meta, "stats");
            CollectPlan(meta, "plan");
            CollectProfile(meta, "profile");
            CollectNotifications(meta, "notifications");
        }


        private void CollectKeys(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }

            var keys = meta.GetValue(name, new List<object>()).Cast<string>();
            _keys = keys.ToArray();
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

        private void CollectStatistics(IDictionary<string, object> meta, string name)
        {
            if (!meta.ContainsKey(name))
            {
                return;
            }
            var stats = meta[name] as IDictionary<string, object>;

            _summaryBuilder.UpdateStatistics = new UpdateStatistics(
                StatsValue(stats, "nodes-created"),
                StatsValue(stats, "nodes-deleted"),
                StatsValue(stats, "relationships-created"),
                StatsValue(stats, "relationships-deleted"),
                StatsValue(stats, "properties-set"),
                StatsValue(stats, "labels-added"),
                StatsValue(stats, "labels-removed"),
                StatsValue(stats, "indexes-added"),
                StatsValue(stats, "indexes-removed"),
                StatsValue(stats, "constraints-added"),
                StatsValue(stats, "constraints-removed"));
        }

        private void CollectPlan(IDictionary<string, object> meta, string name)
        {
            if (meta == null || !meta.ContainsKey(name))
            {
                return;
            }
            var planDict = meta[name] as IDictionary<string, object>;
            _summaryBuilder.Plan = CollectPlan(planDict);
        }

        //private const string MetaName_Plan = "plan";

        private IPlan CollectPlan(IDictionary<string, object> planDict)
        {
            if (planDict == null || planDict.Count == 0)
            {
                return null;
            }
            var operationType = planDict.GetValue("operatorType", string.Empty);
            var args = planDict.GetValue("args", new Dictionary<string, object>());
            var identifiers = planDict.GetValue("identifiers", new List<object>()).Cast<string>();
            var children = planDict.GetValue("children", new List<object>());

            var childPlans = children
                .Select(child => child as IDictionary<string, object>)
                .Select(CollectPlan)
                .Where(childPlan => childPlan != null)
                .ToList();
            return new Plan(operationType, args, identifiers.ToList(), childPlans);
        }

        private IProfiledPlan CollectProfile(IDictionary<string, object> profileDict)
        {
            if (profileDict == null || profileDict.Count == 0)
            {
                return null;
            }
            var operationType = profileDict.GetValue("operatorType", string.Empty);
            var args = profileDict.GetValue("args", new Dictionary<string, object>());
            var identifiers = profileDict.GetValue("identifiers", new List<object>()).Cast<string>();
            var dbHits = profileDict.GetValue("dbHits", 0L);
            var rows = profileDict.GetValue("rows", 0L);
            var children = profileDict.GetValue("children", new List<object>());

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
            var notifiactions = new List<INotification>();
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
                notifiactions.Add(new Notification(code, title, description, position));
            }
            _summaryBuilder.Notifications = notifiactions;
        }

        private static int StatsValue(IDictionary<string, object> stats, string name)
        {
            return (int)stats.GetValue(name, 0L);
        }

        private static StatementType FromCode(string type)
        {
            switch (type)
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