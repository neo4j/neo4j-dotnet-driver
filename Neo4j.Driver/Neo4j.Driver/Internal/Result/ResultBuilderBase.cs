// Copyright (c) "Neo4j"
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
using System;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal abstract class ResultBuilderBase : IMessageResponseCollector
    {
        protected bool StatementProcessed { get; set; } = false;
        protected List<string> Keys { get; } = new List<string>();
        protected SummaryCollector SummaryCollector { get; }
        protected Bookmark Bookmark { get; private set; }

        protected ResultBuilderBase(SummaryCollector summaryCollector)
        {
            SummaryCollector = summaryCollector;
        }

        public void CollectFields(IDictionary<string, object> meta)
        {
            if (meta == null)
            {
                return;
            }

            CollectKeys(meta, "fields", Keys);
            SummaryCollector.CollectWithFields(meta);
        }

        public void CollectBookmark(IDictionary<string, object> meta)
        {
            Bookmark = SummaryCollector.CollectBookmark(meta);
        }

        public void CollectRecord(object[] fields)
        {
            var record = new Record(Keys, fields);
            EnqueueRecord(record);
        }

        public void CollectSummary(IDictionary<string, object> meta)
        {
            NoMoreRecords();
            if (meta == null)
            {
                return;
            }
            SummaryCollector.Collect(meta);
        }

        public void DoneSuccess()
        {
            // do nothing
            StatementProcessed = true;
        }

        public void DoneFailure()
        {
            NoMoreRecords();// an error received, so the result is broken
            StatementProcessed = true;
        }

        public void DoneIgnored()
        {
            NoMoreRecords();// the result is ignored
            StatementProcessed = true;
        }

        protected abstract void NoMoreRecords();
        protected abstract void EnqueueRecord(Record record);

        private static void CollectKeys(IDictionary<string, object> meta, string name, List<string> keys)
        {
            if (meta.ContainsKey(name))
            {
                keys.AddRange(meta[name].As<List<string>>());
            }
        }
    }
}
