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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public class PackStreamMessageFormatV1
    {
        private static BitConverterBase _bitConverter;

        public PackStreamMessageFormatV1(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _bitConverter = bitConverter;
            Writer = new WriterV1(new ChunkedOutputStream(tcpSocketClient, bitConverter));
            Reader = new ReaderV1(new ChunkedInputStream(tcpSocketClient, bitConverter));
        }

        public IWriter Writer { get; }
        public IReader Reader { get; }

        public class ReaderV1 : IReader
        {


            private static readonly IDictionary<string, object> EmptyStringValueMap = new Dictionary<string, object>();
            private readonly IInputStream _inputStream;
            private readonly PackStream.Unpacker _unpacker;

            public ReaderV1(IInputStream inputStream)
            {
                _inputStream = inputStream;
                _unpacker = new PackStream.Unpacker(_inputStream, _bitConverter);
            }

            public bool HasNext()
            {
                throw new NotImplementedException();
            }

            public void Read(IMessageResponseHandler responseHandler)
            {
                _unpacker.UnpackStructHeader();
                var type = _unpacker.UnpackStructSignature();

                switch (type)
                {
/*                    case MSG_RUN:
//                        unpackRunMessage(handler);
                        break;
                    case MSG_DISCARD_ALL:
//                        unpackDiscardAllMessage(handler);
                        break;
                    case MSG_PULL_ALL:
//                        unpackPullAllMessage(handler);
                        break;*/
                    case MSG_RECORD:
                        UnpackRecordMessage(responseHandler);
                        break;
                    case MSG_SUCCESS:
                        UnpackSuccessMessage(responseHandler);
                        break;
                    case MSG_FAILURE:
                        UnpackFailureMessage(responseHandler);
                        break;
//                    case MSG_IGNORED:
////                        unpackIgnoredMessage(handler);
//                        break;
//                    case MSG_INIT:
////                        unpackInitMessage(handler);
//                        break;
                    default:
                        throw new IOException("Unknown message type: " + type);
                }
                UnPackMessageTail();
            }
            public dynamic UnpackValue()
            {
                var type = _unpacker.PeekNextType();
                switch (type)
                {
                    //                    case BYTES:
                    //                        break;
                    //                    case NULL:
                    //                        return value(unpacker.unpackNull());
                    //                    case BOOLEAN:
                    //                        return value(unpacker.unpackBoolean());
                    case PackStream.PackType.Integer:
                        return _unpacker.UnpackLong();
                    //                    case FLOAT:
                    //                        return value(unpacker.unpackDouble());
                    case PackStream.PackType.String:
                        return _unpacker.UnpackString();

                    case PackStream.PackType.Map:
                        {
                            return UnpackMap();
                        }
                    case PackStream.PackType.List:
                        {
                            var size = (int)_unpacker.UnpackListHeader();
                            var vals = new object[size];
                            for (var j = 0; j < size; j++)
                            {
                                vals[j] = UnpackValue();
                            }
                            return new List<object>(vals);
                        }
                        //                    case STRUCT:
                        //                        {
                        //                            long size = unpacker.unpackStructHeader();
                        //                            switch (unpacker.unpackStructSignature())
                        //                            {
                        //                                case NODE:
                        //                                    ensureCorrectStructSize("NODE", NODE_FIELDS, size);
                        //                                    return new NodeValue(unpackNode());
                        //                                case RELATIONSHIP:
                        //                                    ensureCorrectStructSize("RELATIONSHIP", 5, size);
                        //                                    return unpackRelationship();
                        //                                case PATH:
                        //                                    ensureCorrectStructSize("PATH", 3, size);
                        //                                    return unpackPath();
                        //                            }
                        //                        }
                }
                throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown value type: {type}");
            }




            private void UnpackFailureMessage(IMessageResponseHandler responseHandler)
            {
                var values = UnpackMap();
                var code = values["code"]?.ToString(); // TODO
                var message = values["message"]?.ToString();
                responseHandler.HandleFailureMessage(code, message);
            }

            private void UnpackRecordMessage(IMessageResponseHandler responseHandler)
            {
                int fieldCount = (int)_unpacker.UnpackListHeader();
                dynamic[] fields = new dynamic[fieldCount];
                for (int i = 0; i < fieldCount; i++)
                {
                    fields[i] = UnpackValue();
                }
                responseHandler.HandleRecordMessage(fields);
            }

            private void UnPackMessageTail()
            {
                _inputStream.ReadMessageEnding();
            }

            private void UnpackSuccessMessage(IMessageResponseHandler responseHandler)
            {
                var map = UnpackMap();
                responseHandler.HandleSuccessMessage(map);
            }

            //TODO should this be readonly?
            private IDictionary<string, object> UnpackMap()
            {
                var size = (int)_unpacker.UnpackMapHeader();
                if (size == 0)
                {
                    return EmptyStringValueMap;
                }
                IDictionary<string, object> map = new Dictionary<string, object>(size);
                for (var i = 0; i < size; i++)
                {
                    var key = _unpacker.UnpackString();
                    map.Add(key, UnpackValue());
                }
                return map;
            }


        }

        public class WriterV1 : IWriter, IMessageRequestHandler
        {
            private readonly IOutputStream _outputStream;
            private readonly PackStream.Packer _packer;

            public WriterV1(IOutputStream outputStream)
            {
                _outputStream = outputStream;
                _packer = new PackStream.Packer(_outputStream, _bitConverter);
            }

            public void HandleInitMessage(string clientNameAndVersion)
            {
                _packer.PackStructHeader(1, MSG_INIT);
                _packer.Pack(clientNameAndVersion);
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

            public void Write(IMessage message)
            {
                message.Dispatch(this);
//                HandleInitMessage(message);
            }

            public void Flush()
            {
                _outputStream.Flush();
            }

            private void PackMessageTail()
            {
                _outputStream.WriteMessageEnding();
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
                // TODO when we need params in run
                if (value == null)
                {
                    _packer.PackNull();
                    return;
                }

                //If we bring in Nullable<int> (shorthand = int?) we want the underlying type, 
                // here we get the underlying type, BUT if it's *not* a nullable, we just get
                // the normal type.  
                var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
                if (type == typeof (long) || type == typeof (int) || type == typeof (short) || type == typeof (sbyte))
                {
                    _packer.Pack(Convert.ToInt64(value));
                }
                else if (value is bool)
                {
                    _packer.Pack((bool) value);
                }
                else if (type == typeof (double) || type == typeof (float) || type == typeof (decimal))
                {
                    _packer.Pack(Convert.ToDouble(value));
                }
                else if (value is string)
                {
                    _packer.Pack((string) value);
                }
                else if (value is IList)
                {
                    _packer.Pack((IList) value);
                }
                else if (value is IDictionary<string, object>)
                {
                    PackRawMap((IDictionary<string, object>) value);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value.GetType(),
                        $"Cannot understand {nameof(value)} with type {value.GetType().FullName}");

                }

            }
        }

        #region Consts

            public const byte MSG_INIT = 0x01;
        public const byte MSG_ACK_FAILURE = 0x0F;
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

//        public const byte TINY_STRING = 0x80;
//        public const byte TINY_LIST = 0x90;
//        public const byte TINY_MAP = 0xA0;
//        public const byte TINY_STRUCT = 0xB0;
//        public const byte NULL = 0xC0;
//        public const byte FLOAT_64 = 0xC1;
//        public const byte FALSE = 0xC2;
//        public const byte TRUE = 0xC3;
//        public const byte RESERVED_C4 = 0xC4;
//        public const byte RESERVED_C5 = 0xC5;
//        public const byte RESERVED_C6 = 0xC6;
//        public const byte RESERVED_C7 = 0xC7;
//        public const byte INT_8 = 0xC8;
//        public const byte INT_16 = 0xC9;
//        public const byte INT_32 = 0xCA;
//        public const byte INT_64 = 0xCB;
//        public const byte BYTES_8 = 0xCC;
//        public const byte BYTES_16 = 0xCD;
//        public const byte BYTES_32 = 0xCE;
//        public const byte RESERVED_CF = 0xCF;
//        public const byte STRING_8 = 0xD0;
//        public const byte STRING_16 = 0xD1;
//        public const byte STRING_32 = 0xD2;
//        public const byte RESERVED_D3 = 0xD3;
//        public const byte LIST_8 = 0xD4;
//        public const byte LIST_16 = 0xD5;
//        public const byte LIST_32 = 0xD6;
//        public const byte RESERVED_D7 = 0xD7;
//        public const byte MAP_8 = 0xD8;
//        public const byte MAP_16 = 0xD9;
//        public const byte MAP_32 = 0xDA;
//        public const byte RESERVED_DB = 0xDB;
//        public const byte STRUCT_8 = 0xDC;
//        public const byte STRUCT_16 = 0xDD;
//        public const byte RESERVED_DE = 0xDE; // TODO STRUCT_32? or the class javadoc is wrong?
//        public const byte RESERVED_DF = 0xDF;
//        public const byte RESERVED_E0 = 0xE0;
//        public const byte RESERVED_E1 = 0xE1;
//        public const byte RESERVED_E2 = 0xE2;
//        public const byte RESERVED_E3 = 0xE3;
//        public const byte RESERVED_E4 = 0xE4;
//        public const byte RESERVED_E5 = 0xE5;
//        public const byte RESERVED_E6 = 0xE6;
//        public const byte RESERVED_E7 = 0xE7;
//        public const byte RESERVED_E8 = 0xE8;
//        public const byte RESERVED_E9 = 0xE9;
//        public const byte RESERVED_EA = 0xEA;
//        public const byte RESERVED_EB = 0xEB;
//        public const byte RESERVED_EC = 0xEC;
//        public const byte RESERVED_ED = 0xED;
//        public const byte RESERVED_EE = 0xEE;
//        public const byte RESERVED_EF = 0xEF;
//
//        private const long PLUS_2_TO_THE_31 = 2147483648L;
//        private const long PLUS_2_TO_THE_15 = 32768L;
//        private const long PLUS_2_TO_THE_7 = 128L;
//        private const long MINUS_2_TO_THE_4 = -16L;
//        private const long MINUS_2_TO_THE_7 = -128L;
//        private const long MINUS_2_TO_THE_15 = -32768L;
//        private const long MINUS_2_TO_THE_31 = -2147483648L;

        #endregion Consts
    }
}