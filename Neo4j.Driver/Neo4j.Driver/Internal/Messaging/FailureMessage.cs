using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Messaging
{
    class FailureMessage : IMessage
    {
        private readonly string _code;
        private readonly string _message;
        public FailureMessage(string code, string message)
        {
            _code = code;
            _message = message;
        }

        public override string ToString()
        {
            return $"FAILURE code={_code}, message={_message}";
        }
    }
}
