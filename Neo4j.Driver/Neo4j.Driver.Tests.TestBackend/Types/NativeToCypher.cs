using Microsoft.VisualBasic.CompilerServices;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class CypherObject
    {
        public string name { get; set; }
        public DataType data { get; set; } = new DataType();

        public class DataType
        {
            public object value { get; set; }
        }
    }

    internal static class NativeToCypher
    {
        //Mapping of object type to a cypher type name string that will be used in the JSON.
        private static Dictionary<Type, string> TypeMap { get; set; } = new Dictionary<Type, string>() 
        {
            { typeof(List<object>),                 "CypherList" },
            { typeof(Dictionary<string, object>),   "CypherMap" },

            { typeof(bool),                         "CypherBool" },
            { typeof(long),                         "CypherInt" },
            { typeof(double),                       "CypherFloat" },
            { typeof(string),                       "CypherString" },           
            { typeof(byte[]),                       "CypherByteArray" },

            { typeof(LocalDate),                    "CypherDate" },
            { typeof(OffsetTime),                   "CypherTime" },
            { typeof(LocalTime),                    "CypherLocalTime" },
            { typeof(ZonedDateTime),                "CypherDateTime" },
            { typeof(LocalDateTime),                "CypherLocalDateTime" },
            { typeof(Duration),                     "CypherDuration" },
            { typeof(Point),                        "CypherPoint" },

            { typeof(INode),                        "CypherNode" },
            { typeof(IRelationship),                "CypherRelationship" },
            { typeof(IPath),                        "CypherPath" }
        };
        
        //Mapping of object type to a converstion delegate that will return a CypherObject that can be serialized to JSON.
        private static Dictionary<Type, Func<string, object, CypherObject>> FunctionMap { get; set; } = new Dictionary<Type, Func<string, object, CypherObject>>()
        {
            { typeof(List<object>),                 CypherList },
            { typeof(Dictionary<string, object>),   CypherMap },

            { typeof(bool),                         CypherSimple },
            { typeof(long),                         CypherSimple },
            { typeof(double),                       CypherSimple },
            { typeof(string),                       CypherSimple },
            { typeof(byte[]),                       CypherSimple },

            { typeof(LocalDate),                    CypherTODO },
            { typeof(OffsetTime),                   CypherTODO },
            { typeof(LocalTime),                    CypherTODO },
            { typeof(ZonedDateTime),                CypherTODO },
            { typeof(LocalDateTime),                CypherTODO },
            { typeof(Duration),                     CypherTODO },
            { typeof(Point),                        CypherTODO },

            { typeof(INode),                        CypherNode },   //TODO... Needs to be of type Node.
            { typeof(IRelationship),                CypherTODO },
            { typeof(IPath),                        CypherTODO }


        };


        public static CypherObject Convert(IReadOnlyDictionary<string, object> collection)
        {
            Dictionary<string, object> simpleDictionary = new Dictionary<string, object>(collection);

            return InternalConvert(simpleDictionary);            
        }

        public static CypherObject InternalConvert(object sourceObject)
        {
            if (sourceObject is null)
            {
                return new CypherObject { name = "NullRecord", data = {} };
            }

            
            string cypherType = TypeMap[sourceObject.GetType()];
            var function = FunctionMap[sourceObject.GetType()];

            return function(cypherType, sourceObject);
        }


        public static CypherObject CypherSimple(string cypherType, object obj)
        {
            return new CypherObject { name = cypherType, data = { value = obj } };
        }

        public static CypherObject CypherMap(string cypherType, object obj)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            foreach(KeyValuePair<string, object> pair in (Dictionary<string, object>)obj)
            {
                result[pair.Key] = InternalConvert(pair.Value);
            }

            return new CypherObject { name = cypherType, data = { value = result } };
        }

        public static CypherObject CypherList(string cypherType, object obj)
        {
            List<object> result = new List<object>();

            foreach (object item in (List<object>)obj)
            {
                result.Add(InternalConvert(item));
            }

            return new CypherObject { name = cypherType, data = { value = result } };
        }

        public static CypherObject CypherTODO(string name, object obj)
        {
            throw new NotImplementedException($"NativeToCypher : {name} conversion is not implemented yet");
        }

        public static CypherObject CypherNode(string cypherType, object obj)
        {
            var node = (INode)obj;
            var cypherNode = new Dictionary<string, object>
            {
                ["id"] = InternalConvert(node.Id),
                ["labels"] = InternalConvert(new List<string>(node.Labels)),
                ["props"] = InternalConvert(new Dictionary<string, object>(node.Properties))
            };

            return new CypherObject() { name = "Node", data = { value = cypherNode } };
        }
    }
}



