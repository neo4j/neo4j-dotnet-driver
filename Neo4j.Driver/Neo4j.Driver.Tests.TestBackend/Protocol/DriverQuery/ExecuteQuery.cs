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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ExecuteQuery : IProtocolObject
{
    public ExecuteQueryDto data { get; set; }

    [JsonIgnore]
    public EagerResult<IReadOnlyList<IRecord>> Result { get; set; }

    public override async Task Process()
    {
        var driver = ObjManager.GetObject<NewDriver>(data.driverId).Driver;
        var queryConfig = BuildConfig();

        var queryResult = await driver
            .ExecutableQuery(data.cypher)
            .WithParameters(data.parameters)
            .WithConfig(queryConfig)
            .ExecuteAsync();

        Result = queryResult;
    }

    private QueryConfig BuildConfig()
    {
        if (data.config == null)
        {
            return null;
        }

        var routingControl = data.config.routing?.Equals("w", StringComparison.OrdinalIgnoreCase) ?? true
            ? RoutingControl.Writers
            : RoutingControl.Readers;

        var bookmarkManager = default(IBookmarkManager);
        var enableBookmarkManager = true;

        if (!string.IsNullOrEmpty(data.config.bookmarkManagerId))
        {
            if (data.config.bookmarkManagerId == "-1")
            {
                enableBookmarkManager = false;
            }
            else
            {
                bookmarkManager = ObjManager.GetObject<NewBookmarkManager>(data.config.bookmarkManagerId)
                    .BookmarkManager;
            }
        }

        var transactionConfig = new TransactionConfig
        {
            Timeout = data.config.timeout.HasValue ? TimeSpan.FromMilliseconds(data.config.timeout.Value) : null,
            Metadata = data.config.txMeta ?? new Dictionary<string, object>()
        };

        return new QueryConfig(
            routingControl,
            data.config.database,
            data.config.impersonatedUser,
            bookmarkManager,
            enableBookmarkManager,
            transactionConfig);
    }

    public override string Respond()
    {
        var mappedList = Result.Result
            .Select(
                x => new
                {
                    values = x.Values
                        .Select(y => NativeToCypher.Convert(y.Value))
                        .ToList()
                })
            .ToList();

        return new ProtocolResponse(
            "EagerResult",
            new
            {
                keys = Result.Keys,
                records = mappedList,
                summary = SummaryJsonSerializer.SerializeToRaw(Result.Summary)
            }).Encode();
    }

    public class ExecuteQueryDto
    {
        public string driverId { get; set; }
        public string cypher { get; set; }

        [JsonProperty("params")]
        [JsonConverter(typeof(QueryParameterConverter.FullQueryParameterConverter))]
        public Dictionary<string, object> parameters { get; set; }

        public ExecuteQueryConfigDto config { get; set; }
    }

    public class ExecuteQueryConfigDto
    {
        public string routing { get; set; }
        public string database { get; set; }
        public string impersonatedUser { get; set; }
        public string bookmarkManagerId { get; set; }
        public int? timeout { get; set; }
        public Dictionary<string, object> txMeta { get; set; }
    }
}
