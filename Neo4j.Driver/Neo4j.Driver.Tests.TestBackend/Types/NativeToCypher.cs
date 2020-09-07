using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Types;
using System.IO;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NativeToCypherObject
    {
        public string name { get; set; }
        public object data { get; set; }

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
            { typeof(List<object>),                     "CypherList" },
            { typeof(Dictionary<string, object>),       "CypherMap" },

            { typeof(bool),                             "CypherBool" },
            { typeof(long),                             "CypherInt" },
            { typeof(double),                           "CypherFloat" },
            { typeof(string),                           "CypherString" },           
            { typeof(byte[]),                           "CypherByteArray" },

            { typeof(LocalDate),                        "CypherDate" },
            { typeof(OffsetTime),                       "CypherTime" },
            { typeof(LocalTime),                        "CypherLocalTime" },
            { typeof(ZonedDateTime),                    "CypherDateTime" },
            { typeof(LocalDateTime),                    "CypherLocalDateTime" },
            { typeof(Duration),                         "CypherDuration" },
            { typeof(Point),                            "CypherPoint" },

            { typeof(Node),                             "CypherNode" },
            { typeof(Relationship),                     "CypherRelationship" },
            { typeof(Neo4j.Driver.Internal.Types.Path), "CypherPath" }
        };
        
        //Mapping of object type to a conversion delegate that will return a NativeToCypherObject that can be serialized to JSON.
        private static Dictionary<Type, Func<string, object, NativeToCypherObject>> FunctionMap { get; set; } = new Dictionary<Type, Func<string, object, NativeToCypherObject>>()
        {
            { typeof(List<object>),                     CypherList },
            { typeof(Dictionary<string, object>),       CypherMap },

            { typeof(bool),                             CypherSimple },
            { typeof(long),                             CypherSimple },
            { typeof(double),                           CypherSimple },
            { typeof(string),                           CypherSimple },
            { typeof(byte[]),                           CypherSimple },

            { typeof(LocalDate),                        CypherTODO },
            { typeof(OffsetTime),                       CypherTODO },
            { typeof(LocalTime),                        CypherTODO },
            { typeof(ZonedDateTime),                    CypherTODO },
            { typeof(LocalDateTime),                    CypherTODO },
            { typeof(Duration),                         CypherTODO },
            { typeof(Point),                            CypherTODO },

            { typeof(Node),                             CypherNode },   
            { typeof(Relationship),                     CypherTODO },
            { typeof(Neo4j.Driver.Internal.Types.Path), CypherTODO }
        };


        public static NativeToCypherObject Convert(object sourceObject)
        {
            if (sourceObject is null)
            {
                return new NativeToCypherObject { name = "CypherNull", data = {} };
            }

            try
            {
                string cypherType = TypeMap[sourceObject.GetType()];
                var function = FunctionMap[sourceObject.GetType()];

                return function(cypherType, sourceObject);
            }
            catch
            {
                throw new IOException($"Attempting to convert an unsuported object type to a CypherType: {sourceObject.GetType()}");
            }
        }


        public static NativeToCypherObject CypherSimple(string cypherType, object obj)
        {
            return new NativeToCypherObject { name = cypherType, data = new NativeToCypherObject.DataType { value = obj } };
        }

        public static NativeToCypherObject CypherMap(string cypherType, object obj)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            foreach(KeyValuePair<string, object> pair in (Dictionary<string, object>)obj)
            {
                result[pair.Key] = Convert(pair.Value);
            }

            return new NativeToCypherObject { name = cypherType, data = new NativeToCypherObject.DataType{ value = result } };
        }

        public static NativeToCypherObject CypherList(string cypherType, object obj)
        {
            List<object> result = new List<object>();

            foreach (object item in (List<object>)obj)
            {
                result.Add(Convert(item));
            }

            return new NativeToCypherObject { name = cypherType, data = new NativeToCypherObject.DataType { value = result } };
        }

        public static NativeToCypherObject CypherTODO(string name, object obj)
        {
            throw new NotImplementedException($"NativeToCypher : {name} conversion is not implemented yet");
        }

        public static NativeToCypherObject CypherNode(string cypherType, object obj)
        {
            var node = (Node)obj;
            var cypherNode = new Dictionary<string, object>
            {
                ["id"] = Convert(node.Id),
                ["labels"] = Convert(new List<object>(node.Labels)),
                ["props"] = Convert(new Dictionary<string, object>(node.Properties))
            };

            return new NativeToCypherObject() { name = "Node",  data = cypherNode };
        }
    }
}



