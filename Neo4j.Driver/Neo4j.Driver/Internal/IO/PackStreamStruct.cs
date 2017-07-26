using System.Collections;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.IO
{
    public class PackStreamStruct
    {
        public PackStreamStruct(byte signature, IEnumerable<object> fields)
        {
            Signature = signature;
            Fields = new List<object>(fields);
        }

        public byte Signature { get; }

        public IList Fields { get; }

    }
}