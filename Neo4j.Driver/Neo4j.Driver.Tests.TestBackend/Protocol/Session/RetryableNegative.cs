using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class RetryableNegative : IProtocolObject
{
    public RetryableNegativeType data { get; set; } = new();

    public override async Task Process(Controller controller)
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
        sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNegative, data.errorId);

        TriggerEvent();

        await Task.CompletedTask;
    }

    public override string Respond()
    {
        return string.Empty;
    }

    public class RetryableNegativeType
    {
        public string sessionId { get; set; }
        public string errorId { get; set; }
    }
}
