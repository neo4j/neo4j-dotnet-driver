using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoutingSession : ISession
    {
        private ISession _delegate;
        public void Dispose()
        {
            _delegate.Dispose();
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters = null)
        {
            return _delegate.Run(statement, parameters);
        }

        public IStatementResult Run(Statement statement)
        {
            return _delegate.Run(statement);
        }

        public IStatementResult Run(string statement, object parameters)
        {
            return _delegate.Run(statement, parameters);
        }

        public ITransaction BeginTransaction()
        {
            return _delegate.BeginTransaction();
        }

        public void Reset()
        {
            _delegate.Reset();
        }

        public string Server()
        {
            return _delegate.Server();
        }
    }
}
