using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Internal.IO
{
    internal static partial class PackStream
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
        public const byte RESERVED_DE = 0xDE;
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

        public const long PLUS_2_TO_THE_31 = 2147483648L;
        public const long PLUS_2_TO_THE_15 = 32768L;
        public const long PLUS_2_TO_THE_7 = 128L;
        public const long MINUS_2_TO_THE_4 = -16L;
        public const long MINUS_2_TO_THE_7 = -128L;
        public const long MINUS_2_TO_THE_15 = -32768L;
        public const long MINUS_2_TO_THE_31 = -2147483648L;

        #endregion

        #region Bolt Constants

        public const byte MSG_INIT = 0x01;
        public const byte MSG_ACK_FAILURE = 0x0E;
        public const byte MSG_RESET = 0x0F;
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

        public const int NodeFields = 3;
        public const int RelationshipFields = 5;
        public const int UnboundRelationshipFields = 3;
        public const int PathFields = 3;

        #endregion Consts
    }
}
