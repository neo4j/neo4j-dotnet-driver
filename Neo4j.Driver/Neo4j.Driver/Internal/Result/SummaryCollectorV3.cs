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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class SummaryCollectorV3 : SummaryCollector
    {
        public SummaryCollectorV3(Statement statement, IServerInfo server) : base(statement, server)
        {
        }
        
        protected override void CollectResultAvailableAfter(IDictionary<string, object> meta)
        {
            var name = "t_first";
            if (!meta.ContainsKey(name))
            {
                return;
            }
            SummaryBuilder.ResultAvailableAfter = meta[name].As<long>();
        }

        protected override void CollectResultConsumedAfter(IDictionary<string, object> meta)
        {
            var name = "t_last";
            if (!meta.ContainsKey(name))
            {
                return;
            }
            SummaryBuilder.ResultConsumedAfter = meta[name].As<long>();
        }

    }
}