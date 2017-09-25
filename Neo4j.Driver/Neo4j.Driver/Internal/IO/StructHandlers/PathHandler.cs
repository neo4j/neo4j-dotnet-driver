using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    class PathHandler: IPackStreamStructHandler
    {
        private IPackStreamStructHandler _nodeHandler;

        public PathHandler(IPackStreamStructHandler nodeHandler)
        {
            Throw.ArgumentNullException.IfNull(nodeHandler, nameof(nodeHandler));

            _nodeHandler = nodeHandler;
        }

        public byte Signature => PackStream.Path;

        public object Read(PackStreamReader reader, long size)
        {
            // List of unique nodes
            var uniqNodes = new INode[(int)reader.ReadListHeader()];
            for (int i = 0; i < uniqNodes.Length; i++)
            {
                Throw.ProtocolException.IfNotEqual(NodeFields, reader.ReadStructHeader(), nameof(NodeFields),
                    $"received{nameof(NodeFields)}");
                Throw.ProtocolException.IfNotEqual(PackStream.Node, reader.ReadStructSignature(), nameof(PackStream.Node),
                    $"received{nameof(PackStream.Node)}");
                uniqNodes[i] = (INode)_nodeHandler.Read(reader, NodeFields);
            }

            // List of unique relationships, without start/end information
            var uniqRels = new Relationship[(int)reader.ReadListHeader()];
            for (int i = 0; i < uniqRels.Length; i++)
            {
                Throw.ProtocolException.IfNotEqual(UnboundRelationshipFields, reader.ReadStructHeader(),
                    nameof(UnboundRelationshipFields), $"received{nameof(UnboundRelationshipFields)}");
                Throw.ProtocolException.IfNotEqual(UnboundRelationship, reader.ReadStructSignature(),
                    nameof(UnboundRelationship), $"received{nameof(UnboundRelationship)}");
                var urn = reader.ReadLong();
                var relType = reader.ReadString();
                var props = reader.ReadMap();
                uniqRels[i] = new Relationship(urn, -1, -1, relType, props);
            }

            // Path sequence
            var length = (int)reader.ReadListHeader();

            // Knowing the sequence length, we can create the arrays that will represent the nodes, rels and segments in their "path order"
            var segments = new ISegment[length / 2];
            var nodes = new INode[segments.Length + 1];
            var rels = new IRelationship[segments.Length];

            var prevNode = uniqNodes[0];
            INode nextNode; // Start node is always 0, and isn't encoded in the sequence
            Relationship rel;
            nodes[0] = prevNode;
            for (int i = 0; i < segments.Length; i++)
            {
                int relIdx = (int)reader.ReadLong();
                nextNode = uniqNodes[(int)reader.ReadLong()];
                // Negative rel index means this rel was traversed "inversed" from its direction
                if (relIdx < 0)
                {
                    rel = uniqRels[(-relIdx) - 1]; // -1 because rel idx are 1-indexed
                    rel.SetStartAndEnd(nextNode.Id, prevNode.Id);
                }
                else
                {
                    rel = uniqRels[relIdx - 1];
                    rel.SetStartAndEnd(prevNode.Id, nextNode.Id);
                }

                nodes[i + 1] = nextNode;
                rels[i] = rel;
                segments[i] = new Segment(prevNode, rel, nextNode);
                prevNode = nextNode;
            }

            return new Path(segments.ToList(), nodes.ToList(), rels.ToList());
        }
    }
}
