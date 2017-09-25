using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class NodeHandler: IPackStreamStructHandler
    {
        public byte Signature => PackStream.Node;

        public object Read(PackStreamReader reader, long size)
        {
            var urn = reader.ReadLong();

            var numLabels = (int)reader.ReadListHeader();
            var labels = new List<string>(numLabels);
            for (var i = 0; i < numLabels; i++)
            {
                labels.Add(reader.ReadString());
            }
            var numProps = (int)reader.ReadMapHeader();
            var props = new Dictionary<string, object>(numProps);
            for (var j = 0; j < numProps; j++)
            {
                var key = reader.ReadString();
                props.Add(key, reader.Read());
            }

            return new Node(urn, labels, props);
        }

    }
}
