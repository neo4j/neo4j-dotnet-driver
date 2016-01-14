using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    public interface ISocketClient
    {
        Task Start();
        Task Stop();
        void Send(IEnumerable<IMessage> messages, IMessageResponseHandler responseHandler);
    }
}