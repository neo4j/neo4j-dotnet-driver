//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver.Internal.Packstream
{
    internal class PackStreamMessageFormatV1
    {
        private static BitConverterBase _bitConverter;

        public PackStreamMessageFormatV1(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter, ILogger logger)
        {
            _bitConverter = bitConverter;
            Writer = new WriterV1(new ChunkedOutputStream(tcpSocketClient, bitConverter, logger));
            Reader = new ReaderV1(new ChunkedInputStream(tcpSocketClient, bitConverter, logger));
        }

        public IWriter Writer { get; }
        public IReader Reader { get; }

        public class ReaderV1 : IReader
        {
            private static readonly Dictionary<string, object> EmptyStringValueMap = new Dictionary<string, object>();
            private readonly ChunkedInputStream _inputStream;
            private readonly PackStream.Unpacker _unpacker;

            public ReaderV1(ChunkedInputStream inputStream)
            {
                _inputStream = inputStream;
                _unpacker = new PackStream.Unpacker(_inputStream, _bitConverter);
            }

            public void Read(IMessageResponseHandler responseHandler)
            {
                _unpacker.UnpackStructHeader();
                var type = _unpacker.UnpackStructSignature();

                switch (type)
                {
                    case MSG_RECORD:
                        UnpackRecordMessage(responseHandler);
                        break;
                    case MSG_SUCCESS:
                        UnpackSuccessMessage(responseHandler);
                        break;
                    case MSG_FAILURE:
                        UnpackFailureMessage(responseHandler);
                        break;
                    case MSG_IGNORED:
                        UnpackIgnoredMessage(responseHandler);
                        break;
                    default:
                        throw new IOException("Unknown requestMessage type: " + type);
                }
                UnPackMessageTail();
            }

            public object UnpackValue()
            {
                var type = _unpacker.PeekNextType();
                switch (type)
                {
                    case PackStream.PackType.Bytes:
                        break;
                    case PackStream.PackType.Null:
                        return _unpacker.UnpackNull();
                    case PackStream.PackType.Boolean:
                        return _unpacker.UnpackBoolean();
                    case PackStream.PackType.Integer:
                        return _unpacker.UnpackLong();
                    case PackStream.PackType.Float:
                        return _unpacker.UnpackDouble();
                    case PackStream.PackType.String:
                        return _unpacker.UnpackString();
                    case PackStream.PackType.Map:
                        return UnpackMap();
                    case PackStream.PackType.List:
                        return UnpackList();
                    case PackStream.PackType.Struct:
                        long size = _unpacker.UnpackStructHeader();
                        switch (_unpacker.UnpackStructSignature())
                        {
                            case NODE:
                                Throw.ArgumentException.IfNotEqual(NodeFields, size, nameof(NodeFields), nameof(size));
                                return UnpackNode();
                            case RELATIONSHIP:
                                Throw.ArgumentException.IfNotEqual(RelationshipFields, size, nameof(RelationshipFields), nameof(size));
                                return UnpackRelationship();
                            case PATH:
                                Throw.ArgumentException.IfNotEqual(PathFields, size, nameof(PathFields), nameof(size));
                                return UnpackPath();
                        }
                        break;
                }
                throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown value type: {type}");
            }

            private IPath UnpackPath()
            { 
                // List of unique nodes
                var uniqNodes = new INode[(int) _unpacker.UnpackListHeader()];
                for(int i = 0; i < uniqNodes.Length; i ++)
                {
                    Throw.ArgumentException.IfNotEqual(NodeFields, _unpacker.UnpackStructHeader(), nameof(NodeFields), $"received{nameof(NodeFields)}");
                    Throw.ArgumentException.IfNotEqual(NODE, _unpacker.UnpackStructSignature(),nameof(NODE), $"received{nameof(NODE)}");
                    uniqNodes[i]=UnpackNode();
                }

                // List of unique relationships, without start/end information
                var uniqRels = new Relationship[(int)_unpacker.UnpackListHeader()];
                for (int i = 0; i < uniqRels.Length; i++)
                {
                    Throw.ArgumentException.IfNotEqual( UnboundRelationshipFields, _unpacker.UnpackStructHeader(), nameof(UnboundRelationshipFields), $"received{nameof(UnboundRelationshipFields)}");
                    Throw.ArgumentException.IfNotEqual(UNBOUND_RELATIONSHIP, _unpacker.UnpackStructSignature(), nameof(UNBOUND_RELATIONSHIP), $"received{nameof(UNBOUND_RELATIONSHIP)}");
                    var urn = _unpacker.UnpackLong();
                    var relType = _unpacker.UnpackString();
                    var props = UnpackMap();
                    uniqRels[i]=new Relationship(urn, -1, -1, relType, props);
                }

                // Path sequence
                var length = (int)_unpacker.UnpackListHeader();

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
                    int relIdx = (int)_unpacker.UnpackLong();
                    nextNode = uniqNodes[(int)_unpacker.UnpackLong()];
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
                return new Path(segments.ToList(), nodes.ToList(),rels.ToList());
            }

            private IRelationship UnpackRelationship()
            {
                var urn = _unpacker.UnpackLong();
                var startUrn = _unpacker.UnpackLong();
                var endUrn = _unpacker.UnpackLong();
                var relType = _unpacker.UnpackString();
                var props = UnpackMap();

                return new Relationship(urn, startUrn, endUrn, relType, props);
            }

            private INode UnpackNode()
            {
                var urn = _unpacker.UnpackLong();

                var numLabels = (int)_unpacker.UnpackListHeader();
                var labels = new List<string>(numLabels);
                for (var i = 0; i < numLabels; i++)
                {
                    labels.Add(_unpacker.UnpackString());
                }
                var numProps = (int)_unpacker.UnpackMapHeader();
                var props = new Dictionary<string, object>(numProps);
                for (var j = 0; j < numProps; j++)
                {
                    var key = _unpacker.UnpackString();
                    props.Add(key, UnpackValue());
                }

                return new Node(urn, labels, props);
            }

            private void UnpackIgnoredMessage(IMessageResponseHandler responseHandler)
            {
                responseHandler.HandleIgnoredMessage();
            }


            private void UnpackFailureMessage(IMessageResponseHandler responseHandler)
            {
                var values = UnpackMap();
                var code = values["code"]?.ToString();
                var message = values["message"]?.ToString();
                responseHandler.HandleFailureMessage(code, message);
            }

            private void UnpackRecordMessage(IMessageResponseHandler responseHandler)
            {
                var fieldCount = (int)_unpacker.UnpackListHeader();
                var fields = new object[fieldCount];
                for (var i = 0; i < fieldCount; i++)
                {
                    fields[i] = UnpackValue();
                }
                responseHandler.HandleRecordMessage(fields);
            }

            private void UnPackMessageTail()
            {
                _inputStream.ReadMessageTail();
            }

            private void UnpackSuccessMessage(IMessageResponseHandler responseHandler)
            {
                var map = UnpackMap();
                responseHandler.HandleSuccessMessage(map);
            }

            private Dictionary<string, object> UnpackMap()
            {
                var size = (int)_unpacker.UnpackMapHeader();
                if (size == 0)
                {
                    return EmptyStringValueMap;
                }
                var map = new Dictionary<string, object>(size);
                for (var i = 0; i < size; i++)
                {
                    var key = _unpacker.UnpackString();
                    map.Add(key, UnpackValue());
                }
                return map;
            }

            private IList<object> UnpackList()
            {
                var size = (int)_unpacker.UnpackListHeader();
                var vals = new object[size];
                for (var j = 0; j < size; j++)
                {
                    vals[j] = UnpackValue();
                }
                return new List<object>(vals);
            }
        }

        public class WriterV1 : IWriter, IMessageRequestHandler
        {
            private readonly ChunkedOutputStream _outputStream;
            private readonly PackStream.Packer _packer;
            

            public WriterV1(ChunkedOutputStream outputStream)
            {
                _outputStream = outputStream;
                _packer = new PackStream.Packer(_outputStream, _bitConverter);
            }

            public void HandleInitMessage(string clientNameAndVersion, IDictionary<string, object> authToken)
            {
                _packer.PackStructHeader(1, MSG_INIT);
                _packer.Pack(clientNameAndVersion);
                PackRawMap(authToken);
                PackMessageTail();
            }

            public void HandleRunMessage(string statement, IDictionary<string, object> parameters)
            {
                _packer.PackStructHeader(2, MSG_RUN);
                _packer.Pack(statement);
                PackRawMap(parameters);
                PackMessageTail();
            }

            public void HandlePullAllMessage()
            {
                _packer.PackStructHeader(0, MSG_PULL_ALL);
                PackMessageTail();
            }

            public void HandleDiscardAllMessage()
            {
                _packer.PackStructHeader(0, MSG_DISCARD_ALL);
                PackMessageTail();
            }

            public void HandleResetMessage()
            {
                _packer.PackStructHeader( 0, MSG_RESET );
                PackMessageTail();
            }

            public void Write(IRequestMessage requestMessage)
            {
                requestMessage.Dispatch(this);
            }

            public void Flush()
            {
                _outputStream.Flush();
            }

            private void PackMessageTail()
            {
                _outputStream.WriteMessageTail();
            }

            private void PackRawMap(IDictionary<string, object> dictionary)
            {
                if (dictionary == null || dictionary.Count == 0)
                {
                    _packer.PackMapHeader(0);
                    return;
                }

                _packer.PackMapHeader(dictionary.Count);
                foreach (var item in dictionary)
                {
                    _packer.Pack(item.Key);
                    PackValue(item.Value);
                }
            }


            private void PackValue(object value)
            {
                _packer.Pack(value);
                // the driver should never pack node, relationship or path
            }
        }

        #region Consts

        public const byte MSG_INIT = 0x01;
        public const byte MSG_RESET = 0x0F;
        public const byte MSG_RUN = 0x10;
        public const byte MSG_DISCARD_ALL = 0x2F;
        public const byte MSG_PULL_ALL = 0x3F;

        public const byte MSG_RECORD = 0x71;
        public const byte MSG_SUCCESS = 0x70;
        public const byte MSG_IGNORED = 0x7E;
        public const byte MSG_FAILURE = 0x7F;

        public const byte NODE = (byte) 'N';
        public const byte RELATIONSHIP = (byte) 'R';
        public const byte UNBOUND_RELATIONSHIP = (byte) 'r';
        public const byte PATH = (byte) 'P';

        public const long NodeFields = 3;
        public const long RelationshipFields = 5;
        public const long UnboundRelationshipFields = 3;
        public const long PathFields = 3;

        #endregion Consts
    }
}