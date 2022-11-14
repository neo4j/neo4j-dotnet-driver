using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class RetryablePositive : IProtocolObject
{
    public RetryablePositiveType data { get; set; } = new();

    public override async Task Process()
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
        sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAblePositive);

        //Client succeded and wants to commit. 
        //Notify any subscribers.

        TriggerEvent();

        await Task.CompletedTask;
    }

    public override string Respond()
    {
        return string.Empty;
    }

    public class RetryablePositiveType
    {
        public string sessionId { get; set; }
    }
}
