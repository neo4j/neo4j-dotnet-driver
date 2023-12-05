// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal class GetConnectionPoolMetrics : IProtocolObject
{
    public Request data { get; set; }

    public override string Respond()
    {
        var driver = ObjManager.GetObject<NewDriver>(data.driverId).Driver as Internal.Driver;
        var metrics = driver.Context.Metrics.ConnectionPoolMetrics;
        var address = metrics.Where(x => x.Value.Id.Contains(data.address, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .First();

        return new ProtocolResponse(
            "ConnectionPoolMetrics",
            new { inUse = address.InUse, idle = address.Idle }).Encode();
    }

    public class Request
    {
        public string driverId { get; set; }
        public string address { get; set; }
    }
}
