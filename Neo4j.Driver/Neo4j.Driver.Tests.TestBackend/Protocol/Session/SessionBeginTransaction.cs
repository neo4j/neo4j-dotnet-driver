using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SessionBeginTransaction : IProtocolObject
{
    public SessionBeginTransactionType data { get; set; } = new();

    [JsonIgnore] public string TransactionId { get; set; }

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

    public override async Task Process(Controller controller)
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
        var transaction = await sessionContainer.Session.BeginTransactionAsync(TransactionConfig);
        TransactionId = controller.TransactionManager.AddTransaction(
            new TransactionWrapper(
                transaction,
                async cursor =>
                {
                    var result = ProtocolObjectFactory.CreateObject<Result>();
                    result.ResultCursor = cursor;

                    return await Task.FromResult(result.uniqueId);
                }));
    }

    public override string Respond()
    {
        return new ProtocolResponse("Transaction", TransactionId).Encode();
    }

    [JsonConverter(typeof(BaseSessionTypeJsonConverter<SessionBeginTransactionType>))]
    public class SessionBeginTransactionType : BaseSessionType
    {
    }
}
