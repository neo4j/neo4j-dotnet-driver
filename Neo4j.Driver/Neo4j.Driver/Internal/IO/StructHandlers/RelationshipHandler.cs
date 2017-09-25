using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class RelationshipHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.Relationship;

        public object Read(PackStreamReader reader, long size)
        {
            var urn = reader.ReadLong();
            var startUrn = reader.ReadLong();
            var endUrn = reader.ReadLong();
            var relType = reader.ReadString();
            var props = reader.ReadMap();

            return new Relationship(urn, startUrn, endUrn, relType, props);
        }
    }
}
