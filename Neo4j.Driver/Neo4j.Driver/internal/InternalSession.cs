using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sockets.Plugin;

namespace Neo4j.Driver
{
    public class InternalSession : Session
    {
        
        //private readonly Connection _connection;
        public InternalSession(Uri url, Config config)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<Result> Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            throw new NotImplementedException();
        }
    }
}