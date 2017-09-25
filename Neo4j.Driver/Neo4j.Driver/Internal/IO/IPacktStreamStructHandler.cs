using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO
{
    interface IPackStreamStructHandler
    {

        byte Signature { get; }

        object Read(PackStreamReader reader, long size);
    }
}
