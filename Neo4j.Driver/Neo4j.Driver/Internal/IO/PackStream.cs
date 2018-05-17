// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal static class PackStream
    {
        public enum PackType
        {
            Null,
            Boolean,
            Integer,
            Float,
            Bytes,
            String,
            List,
            Map,
            Struct
        }

        #region PackStream Constants

        public const byte TinyString = 0x80;
        public const byte TinyList = 0x90;
        public const byte TinyMap = 0xA0;
        public const byte TinyStruct = 0xB0;
        public const byte Null = 0xC0;
        public const byte Float64 = 0xC1;
        public const byte False = 0xC2;
        public const byte True = 0xC3;
        public const byte ReservedC4 = 0xC4;
        public const byte ReservedC5 = 0xC5;
        public const byte ReservedC6 = 0xC6;
        public const byte ReservedC7 = 0xC7;
        public const byte Int8 = 0xC8;
        public const byte Int16 = 0xC9;
        public const byte Int32 = 0xCA;
        public const byte Int64 = 0xCB;
        public const byte Bytes8 = 0xCC;
        public const byte Bytes16 = 0xCD;
        public const byte Bytes32 = 0xCE;
        public const byte ReservedCf = 0xCF;
        public const byte String8 = 0xD0;
        public const byte String16 = 0xD1;
        public const byte String32 = 0xD2;
        public const byte ReservedD3 = 0xD3;
        public const byte List8 = 0xD4;
        public const byte List16 = 0xD5;
        public const byte List32 = 0xD6;
        public const byte ReservedD7 = 0xD7;
        public const byte Map8 = 0xD8;
        public const byte Map16 = 0xD9;
        public const byte Map32 = 0xDA;
        public const byte ReservedDb = 0xDB;
        public const byte Struct8 = 0xDC;
        public const byte Struct16 = 0xDD;
        public const byte ReservedDe = 0xDE;
        public const byte ReservedDf = 0xDF;
        public const byte ReservedE0 = 0xE0;
        public const byte ReservedE1 = 0xE1;
        public const byte ReservedE2 = 0xE2;
        public const byte ReservedE3 = 0xE3;
        public const byte ReservedE4 = 0xE4;
        public const byte ReservedE5 = 0xE5;
        public const byte ReservedE6 = 0xE6;
        public const byte ReservedE7 = 0xE7;
        public const byte ReservedE8 = 0xE8;
        public const byte ReservedE9 = 0xE9;
        public const byte ReservedEa = 0xEA;
        public const byte ReservedEb = 0xEB;
        public const byte ReservedEc = 0xEC;
        public const byte ReservedEd = 0xED;
        public const byte ReservedEe = 0xEE;
        public const byte ReservedEf = 0xEF;

        public const long Plus2ToThe31 = 2147483648L;
        public const long Plus2ToThe15 = 32768L;
        public const long Plus2ToThe7 = 128L;
        public const long Minus2ToThe4 = -16L;
        public const long Minus2ToThe7 = -128L;
        public const long Minus2ToThe15 = -32768L;
        public const long Minus2ToThe31 = -2147483648L;

        #endregion

        #region Bolt Constants

        public const byte MsgInit = 0x01;
        public const byte MsgAckFailure = 0x0E;
        public const byte MsgReset = 0x0F;
        public const byte MsgRun = 0x10;
        public const byte MsgDiscardAll = 0x2F;
        public const byte MsgPullAll = 0x3F;

        public const byte MsgRecord = 0x71;
        public const byte MsgSuccess = 0x70;
        public const byte MsgIgnored = 0x7E;
        public const byte MsgFailure = 0x7F;

        public const byte Node = (byte)'N';
        public const byte Relationship = (byte)'R';
        public const byte UnboundRelationship = (byte)'r';
        public const byte Path = (byte)'P';

        #endregion Consts

        #region Helper Methods 

        public static readonly Dictionary<string, object> EmptyDictionary = new Dictionary<string, object>();

        public static void EnsureStructSize(String structName, int expected, long actual)
        {
            if (expected != actual)
            {
                throw new ClientException($"{structName} structures should have {expected} fields, however received {actual} fields.");
            }
        }

        #endregion
    }
}
