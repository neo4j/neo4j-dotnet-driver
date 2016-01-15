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
using Neo4j.Driver.Internal.messaging;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public class PackStreamMessageFormatV1
    {
        private static BitConverterBase _bitConverter;
        public IWriter Writer { get; }
        public IReader Reader { get;  }

        public PackStreamMessageFormatV1(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _bitConverter = bitConverter;
            Writer = new WriterV1(new PackStreamV1ChunkedOutput(tcpSocketClient, bitConverter));
            Reader = new ReaderV1(new PackStreamV1ChunkedInput(tcpSocketClient, bitConverter));
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

        public const byte NODE = (byte)'N';
        public const byte RELATIONSHIP = (byte)'R';
        public const byte UNBOUND_RELATIONSHIP = (byte)'r';
        public const byte PATH = (byte)'P';

        public const byte TINY_STRING = 0x80;
        public const byte TINY_LIST = 0x90;
        public const byte TINY_MAP = 0xA0;
        public const byte TINY_STRUCT = 0xB0;
        public const byte NULL = 0xC0;
        public const byte FLOAT_64 = 0xC1;
        public const byte FALSE = 0xC2;
        public const byte TRUE = 0xC3;
        public const byte RESERVED_C4 = 0xC4;
        public const byte RESERVED_C5 = 0xC5;
        public const byte RESERVED_C6 = 0xC6;
        public const byte RESERVED_C7 = 0xC7;
        public const byte INT_8 = 0xC8;
        public const byte INT_16 = 0xC9;
        public const byte INT_32 = 0xCA;
        public const byte INT_64 = 0xCB;
        public const byte BYTES_8 = 0xCC;
        public const byte BYTES_16 = 0xCD;
        public const byte BYTES_32 = 0xCE;
        public const byte RESERVED_CF = 0xCF;
        public const byte STRING_8 = 0xD0;
        public const byte STRING_16 = 0xD1;
        public const byte STRING_32 = 0xD2;
        public const byte RESERVED_D3 = 0xD3;
        public const byte LIST_8 = 0xD4;
        public const byte LIST_16 = 0xD5;
        public const byte LIST_32 = 0xD6;
        public const byte RESERVED_D7 = 0xD7;
        public const byte MAP_8 = 0xD8;
        public const byte MAP_16 = 0xD9;
        public const byte MAP_32 = 0xDA;
        public const byte RESERVED_DB = 0xDB;
        public const byte STRUCT_8 = 0xDC;
        public const byte STRUCT_16 = 0xDD;
        public const byte RESERVED_DE = 0xDE; // TODO STRUCT_32? or the class javadoc is wrong?
        public const byte RESERVED_DF = 0xDF;
        public const byte RESERVED_E0 = 0xE0;
        public const byte RESERVED_E1 = 0xE1;
        public const byte RESERVED_E2 = 0xE2;
        public const byte RESERVED_E3 = 0xE3;
        public const byte RESERVED_E4 = 0xE4;
        public const byte RESERVED_E5 = 0xE5;
        public const byte RESERVED_E6 = 0xE6;
        public const byte RESERVED_E7 = 0xE7;
        public const byte RESERVED_E8 = 0xE8;
        public const byte RESERVED_E9 = 0xE9;
        public const byte RESERVED_EA = 0xEA;
        public const byte RESERVED_EB = 0xEB;
        public const byte RESERVED_EC = 0xEC;
        public const byte RESERVED_ED = 0xED;
        public const byte RESERVED_EE = 0xEE;
        public const byte RESERVED_EF = 0xEF;

        private const long PLUS_2_TO_THE_31 = 2147483648L;
        private const long PLUS_2_TO_THE_15 = 32768L;
        private const long PLUS_2_TO_THE_7 = 128L;
        private const long MINUS_2_TO_THE_4 = -16L;
        private const long MINUS_2_TO_THE_7 = -128L;
        private const long MINUS_2_TO_THE_15 = -32768L;
        private const long MINUS_2_TO_THE_31 = -2147483648L;

        #endregion Consts

        public class ReaderV1 : IReader
        {
            private IChunkedInput _chunkedInput;
            private static readonly IDictionary<string, object> EmptyStringValueMap = new Dictionary<string, object>();

            public ReaderV1(IChunkedInput chunkedInput)
            {
                _chunkedInput = chunkedInput;
            }

            public bool HasNext()
            {
                throw new System.NotImplementedException();
            }

            public void Read(IMessageResponseHandler responseHandler)
            {
                UnpackStructHeader();
                var type = UnpackStructSignature();

                switch (type)
                {
                    case MSG_RUN:
//                        unpackRunMessage(handler);
                        break;
                    case MSG_DISCARD_ALL:
//                        unpackDiscardAllMessage(handler);
                        break;
                    case MSG_PULL_ALL:
//                        unpackPullAllMessage(handler);
                        break;
                    case MSG_RECORD:
//                        unpackRecordMessage(handler);
                        break;
                    case MSG_SUCCESS:
                        UnpackSuccessMessage(responseHandler);
                        break;
                    case MSG_FAILURE:
//                        unpackFailureMessage(handler);
                        break;
                    case MSG_IGNORED:
//                        unpackIgnoredMessage(handler);
                        break;
                    case MSG_INIT:
//                        unpackInitMessage(handler);
                        break;
                    default:
                        throw new IOException("Unknown message type: " + type);
                }
                UnPackMessageTail();
            }

            private void UnPackMessageTail()
            {
                _chunkedInput.ReadMessageEnding();
            }

            private void UnpackSuccessMessage(IMessageResponseHandler responseHandler)
            {
                IDictionary<string, object> map = UnpackMap();
                responseHandler.HandleSuccessMessage(map);
            }

            //TODO should this be readonly?
            private IDictionary<string, object> UnpackMap()
            {
                int size = (int)UnpackMapHeader();
                if (size == 0)
                {
                    return EmptyStringValueMap;
                }
                IDictionary<string, object> map = new Dictionary<string, object>(size);
                for (int i = 0; i < size; i++)
                {
                    string key = UnpackString();
                    map.Add(key, UnpackValue());
                }
                return map;
            }

            public enum PackType
            {
                Null, Boolean, Integer, Float, Bytes,
                String, List, Map, Struct
            }

            private object UnpackValue()
            {
                PackType type = PeekNextType();
                switch (type)
                {
//                    case BYTES:
//                        break;
//                    case NULL:
//                        return value(unpacker.unpackNull());
//                    case BOOLEAN:
//                        return value(unpacker.unpackBoolean());
//                    case INTEGER:
//                        return value(unpacker.unpackLong());
//                    case FLOAT:
//                        return value(unpacker.unpackDouble());
                    case PackType.String:
                        return UnpackString();

//                    case MAP:
//                        {
//                            return new MapValue(unpackMap());
//                        }
                    case PackType.List:
                        {
                            int size = (int)UnpackListHeader();
                            object[] vals = new Object[size];
                            for (int j = 0; j < size; j++)
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
                throw new IOException("Unknown value type: " + type);
            }

            private long UnpackListHeader()
            {
                byte markerByte = _chunkedInput.ReadByte();
                byte markerHighNibble = (byte)(markerByte & 0xF0);
                byte markerLowNibble = (byte)(markerByte & 0x0F);

                if (markerHighNibble == TINY_LIST) { return markerLowNibble; }
                switch (markerByte)
                {
                    case LIST_8: return UnpackUint8();
                    case LIST_16: return UnpackUint16();
                    case LIST_32: return UnpackUint32();
                    default: throw new ArgumentOutOfRangeException("markerByte", markerByte,
                        $"Expected a list, but got: {(markerByte & 0xFF).ToString("X2")}");
                }
            }

            private PackType PeekNextType()
            {
                byte markerByte = _chunkedInput.PeekByte();
                byte markerHighNibble = (byte)(markerByte & 0xF0);

                switch (markerHighNibble)
                {
                    case TINY_STRING: return PackType.String;
                    case TINY_LIST: return PackType.List;
                    case TINY_MAP: return PackType.Map;
                    case TINY_STRUCT: return PackType.Struct;
                }

                switch (markerByte)
                {
                    case NULL:
                        return PackType.Null;
                    case TRUE:
                    case FALSE:
                        return PackType.Boolean;
                    case FLOAT_64:
                        return PackType.Float;
                    case BYTES_8:
                    case BYTES_16:
                    case BYTES_32:
                        return PackType.Bytes;
                    case STRING_8:
                    case STRING_16:
                    case STRING_32:
                        return PackType.String;
                    case LIST_8:
                    case LIST_16:
                    case LIST_32:
                        return PackType.List;
                    case MAP_8:
                    case MAP_16:
                    case MAP_32:
                        return PackType.Map;
                    case STRUCT_8:
                    case STRUCT_16:
                        return PackType.Struct;
                    default:
                        return PackType.Integer;
                }
            }
        

            private string UnpackString()
            {
                byte markerByte = _chunkedInput.ReadByte();
                if (markerByte == TINY_STRING) // Note no mask, so we compare to 0x80.
                {
                    return string.Empty;
                }
                
                return _bitConverter.ToString(UnpackUtf8(markerByte));
            }

            private byte[] UnpackUtf8(byte markerByte)
            {
                byte markerHighNibble = (byte)(markerByte & 0xF0);
                byte markerLowNibble = (byte)(markerByte & 0x0F);

                if (markerHighNibble == TINY_STRING) { return UnpackBytes(markerLowNibble); }
                switch (markerByte)
                {
                    case STRING_8: return UnpackBytes(UnpackUint8());
                    case STRING_16: return UnpackBytes(UnpackUint16());
                    case STRING_32:
                        {
                            long size = UnpackUint32();
                            if (size <= int.MaxValue)
                            {
                                return UnpackBytes((int)size);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException("STRING_32 too long for Java");
                            }
                        }
                    default: throw new ArgumentOutOfRangeException( "markerByte", markerByte,
                        $"Expected a string, but got: 0x{(markerByte & 0xFF).ToString("X2")}");
                }
            }

            private byte[] UnpackBytes(int size)
            {
                byte[] heapBuffer = new byte[size];
                _chunkedInput.ReadBytes(heapBuffer);
                return heapBuffer;
            }

            private long UnpackMapHeader()
            {
                byte markerByte = _chunkedInput.ReadByte();
                byte markerHighNibble = (byte)(markerByte & 0xF0);
                byte markerLowNibble = (byte)(markerByte & 0x0F);

                if (markerHighNibble == TINY_MAP) { return markerLowNibble; }
                switch (markerByte)
                {
                    case MAP_8: return UnpackUint8();
                    case MAP_16: return UnpackUint16();
                    case MAP_32: return UnpackUint32();
                    default: throw new ArgumentOutOfRangeException( "markerByte", markerByte,
                        $"Expected a map, but got: {markerByte.ToString("X2")}");
                }

            }

            private long UnpackUint32()
            {
                return _chunkedInput.ReadInt() & 0xFFFFFFFFL;
            }


            private byte UnpackStructSignature()
            {
                return _chunkedInput.ReadByte();
            }

            private long UnpackStructHeader()
            {

                byte markerByte = _chunkedInput.ReadByte();
                byte markerHighNibble = (byte)(markerByte & 0xF0);
                byte markerLowNibble = (byte)(markerByte & 0x0F);

                if (markerHighNibble == TINY_STRUCT) { return markerLowNibble; }
                switch (markerByte)
                {
                    case STRUCT_8: return UnpackUint8();
                    case STRUCT_16: return UnpackUint16();
                    default: throw new ArgumentOutOfRangeException("markerByte", markerByte,
                        $"Expected a struct, but got: {markerByte.ToString("X2")}");
                }
            }

            private int UnpackUint8()
            {
                return _chunkedInput.ReadByte() & 0xFF;
            }

            private int UnpackUint16()
            {
                return _chunkedInput.ReadShort() & 0xFFFF;
            }
        }

        public class WriterV1 : IWriter, IMessageRequestHandler
        {
            private readonly IChunkedOutput _chunkedOutput;

            public WriterV1(IChunkedOutput chunkedOutput)
            {
                _chunkedOutput = chunkedOutput;
            }

            public void HandleInitMessage(string clientNameAndVersion)
            {
                PackStructHeader(1, MSG_INIT);
                Pack(clientNameAndVersion);
                PackMessageTail();
            }

            private void PackMessageTail()
            {
                _chunkedOutput.WriteMessageEnding();
            }

            public void HandleRunMessage(string statement, IDictionary<string, object> parameters)
            {
                PackStructHeader(2, MSG_RUN);
                Pack(statement);
                PackRawMap(parameters);
                PackMessageTail();
            }

            public void HandlePullAllMessage()
            {
                PackStructHeader(0, MSG_PULL_ALL);
                PackMessageTail();
            }

            private void PackRawMap(IDictionary<string, object> dictionary)
            {
                if (dictionary == null || dictionary.Count == 0)
                {
                    PackMapHeader(0);
                    return;
                }

                PackMapHeader(dictionary.Count);
                foreach (var item in dictionary)
                {
                    Pack(item.Key);
                    PackValue(item.Value);
                }
            }

            private void PackValue(object value)
            {
                // TODO when we need params in run
                if (value == null)
                {
                    PackNull();
                    return;
                }
               
                //If we bring in Nullable<int> (shorthand = int?) we want the underlying type, 
                // here we get the underlying type, BUT if it's *not* a nullable, we just get
                // the normal type.  
                var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
                if (type == typeof (long) || type == typeof (int) || type == typeof (short) || type == typeof (sbyte))
                {
                    PackLong(Convert.ToInt64(value));
                }
//                Run("MATCH (n:Movie) WHERE n.Title = {titleParam} AND n.Year = {ageParam} RETURN n", 
//                    new {
//                        titleParam = "MyMovie",
//                        yearParam = myclass.Year
//                    });
//                
            }

            private void PackLong(long value)
            {
                throw new NotImplementedException();
            }

            private void PackMapHeader(int size)
            {
                if (size < 0x10)
                {
                    _chunkedOutput.Write((byte) (TINY_MAP | size));
                }
                else if (size <= byte.MaxValue)
                {
                    _chunkedOutput.Write(MAP_8, (byte) size);
                }
                else if (size <= short.MaxValue)
                {
                    _chunkedOutput.Write(MAP_16, _bitConverter.GetBytes((short) size));
                }
                else
                {
                    _chunkedOutput.Write(MAP_32, _bitConverter.GetBytes(size));
                }
            }

            public void Write(IMessage message)
            {
                message.Dispatch(this);
//                HandleInitMessage(message);
            }

            public void Flush()
            {
                _chunkedOutput.Flush();
            }

            public void Pack(string value)
            {
                if (value == null)
                {
                    PackNull();
                    return;
                }

                var bytes = _bitConverter.GetBytes(value);
                PackStringHeader(bytes.Length);
                _chunkedOutput.Write(bytes);
            }

            private void PackNull()
            {
                _chunkedOutput.Write(NULL);
            }

            public void PackStringHeader(int size)
            {
                if (size < 0x10)
                {
                    _chunkedOutput.Write((byte)(TINY_STRING | size));
                }
                else if (size <= byte.MaxValue)
                {
                    _chunkedOutput.Write(STRING_8, (byte)size);
                }
                else if (size <= short.MaxValue)
                {
                    _chunkedOutput.Write(STRING_16, _bitConverter.GetBytes((short)size));
                }
                else
                {
                    _chunkedOutput.Write(STRING_32, _bitConverter.GetBytes(size));
                }
            }

            private void PackStructHeader(int size, byte signature)
            {
                if (size < 0x10)
                {
                    _chunkedOutput.Write((byte)(TINY_STRUCT | size), signature);
                }
                else if (size <= byte.MaxValue)
                {
                    _chunkedOutput.Write(STRUCT_8, (byte)size, signature);
                }
                else if (size <= short.MaxValue)
                {
                    _chunkedOutput.Write(STRUCT_16, _bitConverter.GetBytes((short)size)).Write(signature);
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(size), size,
                        $"Structures cannot have more than {short.MaxValue} fields");
            }

            //
            //        public byte[] PackBytesHeader(int size)
            //        {
            //            if ( size <= byte.MaxValue )
            //            {
            //                return new byte[] {BYTES_8,(byte)size };
            //            }
            //            if ( size <= short.MaxValue )
            //            {
            //                return new byte[] { BYTES_16, };
            //                out.writeByte(BYTES_16)
            //                   .writeShort((short)size);
            //            }
            //            else
            //            {
            //                out.writeByte(BYTES_32)
            //                   .writeInt(size);
            //            }
            //        }
        }
    }

}