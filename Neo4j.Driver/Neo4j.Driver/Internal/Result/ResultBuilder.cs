﻿//  Copyright (c) 2002-2016 "Neo Technology,"
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
using System;

namespace Neo4j.Driver.Internal.Result
{ 
    public class ResultBuilder : IResultBuilder
    {
        private string[] _keys = new string[0];
        private readonly IList<Record> _records = new List<Record>();
        private readonly SummaryBuilder _summaryBuilder;
        private int _recordIteratorIndex = 0;

        internal bool HasMoreRecords { get; private set; } = true;

        public ResultBuilder() : this(null, null)
        {
        }

        public ResultBuilder(Statement statement)
        {
            _summaryBuilder = new SummaryBuilder(statement);
        }

        public ResultBuilder(string statement, IDictionary<string, object> parameters)
            : this(new Statement(statement, parameters))
        {
        }

        public void Record(object[] fields)
        {
            var record = new Record(_keys, fields);
            _records.Add(record);
        }
       
        private IEnumerable<Record> RecordsStream()
        {
            while (HasMoreRecords || _recordIteratorIndex <= _records.Count)
            {
                while (_recordIteratorIndex == _records.Count)
                {
                    Task.Delay(50).Wait();
                    if (!HasMoreRecords && _recordIteratorIndex == _records.Count)
                        yield break;
                }

                yield return _records[_recordIteratorIndex];
                _recordIteratorIndex++;
            }
        } 

        private Record Peek()
        {
            while (_recordIteratorIndex + 1 >= _records.Count) // Peeking record not received
            {
                if (!HasMoreRecords && _recordIteratorIndex == _records.Count)
                {
                    return null;
                }

                Task.Delay(50).Wait();
            }

            return _records[_recordIteratorIndex + 1];
        }

        public StatementResult Build()
        {
            return new StatementResult(
                _keys, 
                new RecordSet(RecordsStream, Peek, () => _recordIteratorIndex - 1, () => !HasMoreRecords),
                () => _summaryBuilder.Build());
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
            HasMoreRecords = false;
            if (meta == null)
            {
                return;
            }

            CollectType(meta, "type");
            CollectCounters(meta, "stats");
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
                notifications.Add(new Notification(code, title, description, position));
            }
            _summaryBuilder.Notifications = notifications;
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

        // TODO: Verify that there are meaningfull unittests for this
        private class RecordSet : IRecordSet
        {
            private readonly Func<IEnumerable<Record>> _getRecords;
            private readonly Func<Record> _peekRecord;
            private readonly Func<int> _getPosition;
            private readonly Func<bool> _atEnd;

            public RecordSet(Func<IEnumerable<Record>> getRecords, Func<Record> peekRecord, Func<int> getPosition, Func<bool> atEnd)
            {
                _getRecords = getRecords;
                _peekRecord = peekRecord;
                _getPosition = getPosition;
                _atEnd = atEnd;
            }

            /// <summary>
            /// Returns the current position of the last read record.
            /// If no records have been read, this returns -1
            /// </summary>
            public int Position
            {
                get
                {
                    return _getPosition();
                }
            }

            public bool AtEnd
            {
                get
                {
                    return _atEnd();
                }
            }

            public Record Peek
            {
                get
                {
                    return _peekRecord();
                }
            }

            public IEnumerable<Record> Records
            {
                get
                {
                    return _getRecords();
                }
            }
        }
    }
}