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
using Neo4j.Driver.Internal.messaging;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver
{
    public interface IPacker
    {
        void Pack(InitMessage message);
        void Flush();
    }

    public class PackStreamV1Packer : IPacker
    {
        private readonly BitConverterBase _bitConverter;

        private readonly IChunker _chunker;

        public PackStreamV1Packer(ITcpSocketClient tcpSocketClient, BitConverterBase bitConverter)
        {
            _bitConverter = bitConverter;
            _chunker = new PackStreamV1Chunker(tcpSocketClient, bitConverter);
        }

        public void Pack(InitMessage message)
        {
            HandleInitMessage(message);
        }

        public void Flush()
        {
            _chunker.Flush();
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
            _chunker.Write(bytes);
        }

        private void PackNull()
        {
            _chunker.Write(NULL);
        }

        public void HandleInitMessage(InitMessage message)
        {
            PackStructHeader(1, MSG_INIT);
            Pack(message.ClientName);
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

        public void PackStringHeader(int size)
        {
            if (size < 0x10)
            {
                _chunker.Write((byte) (TINY_STRING | size));
            }
            else if (size <= byte.MaxValue)
            {
                _chunker.Write(STRING_8, (byte) size);
            }
            else if (size <= short.MaxValue)
            {
                _chunker.Write(STRING_16, _bitConverter.GetBytes((short) size));
            }
            else
            {
                _chunker.Write(STRING_32, _bitConverter.GetBytes(size));
            }
        }

        private void PackStructHeader(int size, byte signature)
        {
            if (size < 0x10)
            {
                _chunker.Write((byte) (TINY_STRUCT | size), signature);
            }
            else if (size <= byte.MaxValue)
            {
                _chunker.Write(STRUCT_8, (byte) size, signature);
            }
            else if (size <= short.MaxValue)
            {
                _chunker.Write(STRUCT_16, _bitConverter.GetBytes((short) size)).Write(signature);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(size), size,
                    $"Structures cannot have more than {short.MaxValue} fields");
        }

        #region Consts

        public const byte MSG_INIT = 0x01;

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
    }
}