using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class BoltReader: IBoltReader
    {
        private readonly IChunkReader _chunkReader;
        private readonly IPackStreamReader _packStreamReader;
        private readonly ILogger _logger;
        private readonly MemoryStream _bufferStream;

        public BoltReader(Stream stream)
            : this(stream, true)
        {
            
        }

        public BoltReader(Stream stream, bool supportBytes)
            : this(new ChunkReader(stream), null, supportBytes)
        {

        }

        public BoltReader(Stream stream, ILogger logger, bool supportBytes)
            : this(new ChunkReader(stream, logger), logger, supportBytes)
        {

        }

        public BoltReader(IChunkReader chunkReader, ILogger logger, bool supportBytes)
        {
            Throw.ArgumentNullException.IfNull(chunkReader, nameof(chunkReader));

            _logger = logger;
            _chunkReader = chunkReader;
            _bufferStream = new MemoryStream();
            _packStreamReader = supportBytes ? new PackStreamReader(_bufferStream) : new PackStreamReaderBytesIncompatible(_bufferStream);
        }



        public void Read(IMessageResponseHandler responseHandler)
        {
            _bufferStream.SetLength(0);

            _chunkReader.ReadNextChunk(_bufferStream);

            _bufferStream.Position = 0;

            var structure = (PackStreamStruct)_packStreamReader.Read();

            switch (structure.Signature)
            {
                case MSG_RECORD:
                    UnpackRecordMessage(responseHandler, structure);
                    break;
                case MSG_SUCCESS:
                    UnpackSuccessMessage(responseHandler, structure);
                    break;
                case MSG_FAILURE:
                    UnpackFailureMessage(responseHandler, structure);
                    break;
                case MSG_IGNORED:
                    UnpackIgnoredMessage(responseHandler, structure);
                    break;
                default:
                    throw new ProtocolException("Unknown requestMessage type: " + structure.Signature);
            }
        }

        public Task ReadAsync(IMessageResponseHandler responseHandler)
        {
            _bufferStream.SetLength(0);

            return
                _chunkReader.ReadNextChunkAsync(_bufferStream)
                    .ContinueWith(t =>
                    {
                        _bufferStream.Position = 0;

                        var structure = (PackStreamStruct)_packStreamReader.Read();

                        switch (structure.Signature)
                        {
                            case MSG_RECORD:
                                UnpackRecordMessage(responseHandler, structure);
                                break;
                            case MSG_SUCCESS:
                                UnpackSuccessMessage(responseHandler, structure);
                                break;
                            case MSG_FAILURE:
                                UnpackFailureMessage(responseHandler, structure);
                                break;
                            case MSG_IGNORED:
                                UnpackIgnoredMessage(responseHandler, structure);
                                break;
                            default:
                                throw new ProtocolException("Unknown requestMessage type: " + structure.Signature);
                        }
                    });
        }

        private void UnpackIgnoredMessage(IMessageResponseHandler responseHandler, PackStreamStruct structure)
        {
            responseHandler.HandleIgnoredMessage();
        }

        private void UnpackFailureMessage(IMessageResponseHandler responseHandler, PackStreamStruct structure)
        {
            var values = (IDictionary) structure.Fields[0];
            var code = values["code"]?.ToString();
            var message = values["message"]?.ToString();
            responseHandler.HandleFailureMessage(code, message);
        }

        private void UnpackSuccessMessage(IMessageResponseHandler responseHandler, PackStreamStruct structure)
        {
            var map = (IDictionary<string, object>)structure.Fields[0];
            responseHandler.HandleSuccessMessage(map);
        }

        private void UnpackRecordMessage(IMessageResponseHandler responseHandler, PackStreamStruct structure)
        {
            var fieldsList = (IList)structure.Fields[0];

            var fieldCount = fieldsList.Count;
            var fields = new object[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                var field = fieldsList[i];

                if (field is IList)
                {
                    IList list = (IList) field;

                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j] is PackStreamStruct)
                        {
                            list[j] = UnpackStructure((PackStreamStruct) list[j]);
                        }
                    }
                }

                if (field is PackStreamStruct)
                {
                    field = UnpackStructure((PackStreamStruct) field);
                }

                fields[i] = field;
            }

            responseHandler.HandleRecordMessage(fields);
        }

        internal static object UnpackStructure(PackStreamStruct structure)
        {
            var size = structure.Fields.Count;
            switch (structure.Signature)
            {
                case NODE:
                    Throw.ProtocolException.IfNotEqual(NodeFields, size, nameof(NodeFields), nameof(size));
                    return UnpackNode(structure);
                case RELATIONSHIP:
                    Throw.ProtocolException.IfNotEqual(RelationshipFields, size, nameof(RelationshipFields),
                        nameof(size));
                    return UnpackRelationship(structure);
                case PATH:
                    Throw.ProtocolException.IfNotEqual(PathFields, size, nameof(PathFields), nameof(size));
                    return UnpackPath(structure);
            }
            throw new ProtocolException($"Unsupported struct type {structure.Signature}");
        }

        private static IPath UnpackPath(PackStreamStruct structure)
        {
            // List of unique nodes
            var uniqNodesUnchecked = (IList<object>) structure.Fields[0];
            var uniqNodes = new INode[uniqNodesUnchecked.Count];
            for (int i = 0; i < uniqNodes.Length; i++)
            {
                var nodeStruct = (PackStreamStruct) uniqNodesUnchecked[i];

                Throw.ProtocolException.IfNotEqual(NodeFields, nodeStruct.Fields.Count, nameof(NodeFields),
                    $"received{nameof(NodeFields)}");
                Throw.ProtocolException.IfNotEqual(NODE, nodeStruct.Signature, nameof(NODE),
                    $"received{nameof(NODE)}");

                uniqNodes[i] = UnpackNode(nodeStruct);
            }

            // List of unique relationships, without start/end information
            var uniqRelsUnchecked = (IList<object>)structure.Fields[1];
            var uniqRels = new Relationship[uniqRelsUnchecked.Count];
            for (int i = 0; i < uniqRels.Length; i++)
            {
                var relStruct = (PackStreamStruct)uniqRelsUnchecked[i];

                Throw.ProtocolException.IfNotEqual(UnboundRelationshipFields, relStruct.Fields.Count,
                    nameof(UnboundRelationshipFields), $"received{nameof(UnboundRelationshipFields)}");
                Throw.ProtocolException.IfNotEqual(UNBOUND_RELATIONSHIP, relStruct.Signature,
                    nameof(UNBOUND_RELATIONSHIP), $"received{nameof(UNBOUND_RELATIONSHIP)}");

                var urn = Convert.ToInt64(relStruct.Fields[0]);
                var relType = Convert.ToString(relStruct.Fields[1]);
                var props = (IDictionary<string, object>) relStruct.Fields[2];
                uniqRels[i] = new Relationship(urn, -1, -1, relType, new Dictionary<string, object>(props));
            }

            var sequenceList = (IList<object>) structure.Fields[2];
            // Path sequence
            var length = (int)sequenceList.Count;

            // Knowing the sequence length, we can create the arrays that will represent the nodes, rels and segments in their "path order"
            var segments = new ISegment[length / 2];
            var nodes = new INode[segments.Length + 1];
            var rels = new IRelationship[segments.Length];

            var index = 0;
            var prevNode = uniqNodes[0];
            INode nextNode; // Start node is always 0, and isn't encoded in the sequence
            Relationship rel;
            nodes[0] = prevNode;
            for (int i = 0; i < segments.Length; i++)
            {
                int relIdx = (int) Convert.ToInt32(sequenceList[index++]);
                nextNode = uniqNodes[(int)Convert.ToInt32(sequenceList[index++])];
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

        private static IRelationship UnpackRelationship(PackStreamStruct structure)
        {
            var urn = Convert.ToInt64(structure.Fields[0]);
            var startUrn = Convert.ToInt64(structure.Fields[1]);
            var endUrn = Convert.ToInt64(structure.Fields[2]);
            var relType = Convert.ToString(structure.Fields[3]);
            var props = (IDictionary<string, object>)structure.Fields[4];

            return new Relationship(urn, startUrn, endUrn, relType, new Dictionary<string, object>(props));
        }

        private static INode UnpackNode(PackStreamStruct structure)
        {
            var urn = Convert.ToInt64(structure.Fields[0]);

            var labels = ((IList<object>) structure.Fields[1]).Select(o => o.ValueToString());

            var props = ((IDictionary<string, object>) structure.Fields[2]);

            return new Node(urn, labels.ToList(), new Dictionary<string, object>(props));
        }

    }
}
