using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SessionRun : IProtocolObject
{
    public SessionRunType data { get; set; } = new();

    [JsonIgnore] private string ResultId { get; set; }

    private Dictionary<string, object> ConvertParameters(Dictionary<string, CypherToNativeObject> source)
    {
        if (data.parameters == null)
        {
            return null;
        }

        var newParams = new Dictionary<string, object>();

        foreach (var element in source)
        {
            newParams.Add(element.Key, CypherToNative.Convert(element.Value));
        }

        return newParams;
    }

    private void TransactionConfig(TransactionConfigBuilder configBuilder)
    {
        try
        {
            if (data.TimeoutSet)
            {
                var timeout = data.timeout.HasValue
                    ? TimeSpan.FromMilliseconds(data.timeout.Value)
                    : default(TimeSpan?);

                configBuilder.WithTimeout(timeout);
            }
        }
        catch (ArgumentOutOfRangeException e) when ((data.timeout ?? 0) < 0 && e.ParamName == "value")
        {
            throw new DriverExceptionWrapper(e);
        }

        if (data.txMeta.Count > 0)
        {
            configBuilder.WithMetadata(data.txMeta);
        }
    }

    public override async Task Process()
    {
        var newSession = (NewSession)ObjManager.GetObject(data.sessionId);
        var cursor = await newSession.Session
            .RunAsync(data.cypher, ConvertParameters(data.parameters), TransactionConfig)
            .ConfigureAwait(false);

        var result = ProtocolObjectFactory.CreateObject<Result>();
        result.ResultCursor = cursor;

        ResultId = result.uniqueId;
    }

    public override string Respond()
    {
        return ((Result)ObjManager.GetObject(ResultId)).Respond();
    }

    [JsonConverter(typeof(SessionTypeJsonConverter))]
    public class SessionRunType : BaseSessionType
    {
        public string cypher { get; set; }

        [JsonProperty("params")]
        [JsonConverter(typeof(QueryParameterConverter))]
        public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new();
    }
}
