using System.Collections.Generic;

namespace Neo4j.Driver
{
    internal class RunMessage : IMessage
    {
        private readonly string _statement;
        private readonly IDictionary<string, object> _statementParameters;

        public RunMessage(string statement, IDictionary<string, object> statementParameters = null)
        {
            _statement = statement;
            _statementParameters = statementParameters;
        }

        public void Dispatch(IMessageRequestHandler messageRequestHandler)
        {
            messageRequestHandler.HandleRunMessage( _statement, _statementParameters );
        }
    }
}