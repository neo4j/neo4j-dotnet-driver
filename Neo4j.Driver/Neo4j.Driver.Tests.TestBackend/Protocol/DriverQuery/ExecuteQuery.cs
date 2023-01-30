using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Experimental;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ExecuteQuery : IProtocolObject
{
    public ExecuteQueryDto data { get; set; }
    [JsonIgnore]
    public EagerResult Result { get; set; }

    public class ExecuteQueryDto
    {
        public string driverId { get; set; }
        public string cypher { get; set; }
        [JsonProperty("params")]
        [JsonConverter(typeof(FullQueryParameterConverter))]
        public Dictionary<string, object> parameters { get; set; }
        public ExecuteQueryConfigDto config { get; set; }
    }

    public class ExecuteQueryConfigDto
    {
        public string routing { get; set; }
        public string database { get; set; }
        public string impersonatedUser { get; set; }
        public string bookmarkManagerId { get; set; }
    }

    public override async Task Process()
    {
        var driver = ObjManager.GetObject<NewDriver>(data.driverId).Driver;
        var queryConfig = BuildConfig();

        Result = await driver
            .ExecutableQuery(data.cypher)
            .WithParameters(data.parameters)
            .WithConfig(queryConfig)
            .ExecuteAsync();
    }

    private QueryConfig BuildConfig()
    {
        if (data.config == null)
            return null;
        
        var routingControl = data.config.routing?.Equals("w", StringComparison.OrdinalIgnoreCase) ?? true
            ? RoutingControl.Writers
            : RoutingControl.Readers;

        var bookmarkManager = default(IBookmarkManager);
        var enableBookmarkManager = true;

        if (!string.IsNullOrEmpty(data.config.bookmarkManagerId))
        {
            if (data.config.bookmarkManagerId == "-1")
                enableBookmarkManager = false;
            else
                bookmarkManager = ObjManager.GetObject<NewBookmarkManager>(data.config.bookmarkManagerId).BookmarkManager;
        }

        return new QueryConfig(
            routingControl,
            data.config.database,
            data.config.impersonatedUser,
            bookmarkManager, 
            enableBookmarkManager);
    }

    public override string Respond()
    {
        var mappedList = Result.Records
            .Select(x => new
            {
                values = x.Values
                    .Select(y => NativeToCypher.Convert(y.Value))
                    .ToList()
            })
            .ToList();

        return new ProtocolResponse("EagerResult", new
        {
            keys = Result.Keys,
            records = mappedList,
            summary = SummaryJsonSerializer.SerializeToRaw(Result.Summary)
        }).Encode();
    }
}
