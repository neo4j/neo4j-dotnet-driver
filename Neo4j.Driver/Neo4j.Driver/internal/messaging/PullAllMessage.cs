using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.messaging
{
    class PullAllMessage : IMessage
    {
        public void Dispatch(IMessageRequestHandler messageRequestHandler)
        {
            messageRequestHandler.HandlePullAllMessage();
        }
    }
}
