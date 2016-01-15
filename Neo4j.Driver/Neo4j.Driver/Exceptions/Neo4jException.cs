using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver.Exceptions
{
    [DataContract]
    public class Neo4jException : Exception
    {
        public Neo4jException()
        {
        }

        public Neo4jException(string message)
            : base(message)
        {
        }

        public Neo4jException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}