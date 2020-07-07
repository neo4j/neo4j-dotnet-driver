using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RetryableNegative : IProtocolObject
    {
        public RetryableNegativeType data { get; set; } = new RetryableNegativeType();

        public class RetryableNegativeType
        {
            public string errorId { get; set; }
        }

        public override async Task Process()
        {
            await AysncVoidReturn();
        }

        public override string Respond()
        {
            Exception ex = null;
            if (string.IsNullOrEmpty(data.errorId))
            {
                ex = new ClientException("Error from client");
            }
            else
            {
                ex = ((ProtocolException)ObjManager.GetObject(data.errorId)).ExceptionObj;
            }

            return ExceptionManager.GenerateExceptionResponse(ex).Encode();
        }
    }
}