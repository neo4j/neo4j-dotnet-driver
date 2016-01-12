using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    public interface Session : IDisposable
    {
        Task<Result> Run(string statement, IDictionary<string, object> statementParameters = null);
    }
}