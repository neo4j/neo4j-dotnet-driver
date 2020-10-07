using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
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

            { typeof(INode),                             CypherNode },   
            { typeof(IRelationship),                     CypherTODO },
            { typeof(IPath),                             CypherTODO }
        };

        public static object Convert(object sourceObject)
        {
            if (sourceObject is null)
            {
                return new NativeToCypherObject { name = "CypherNull", data = { } };
            }

            if (sourceObject as List<object> != null)
                return FunctionMap[typeof(List<object>)]("CypherList", sourceObject);

            if (sourceObject as Dictionary<string, object> != null)
                return FunctionMap[typeof(Dictionary<string, object>)]("CypherMap", sourceObject);

            if (sourceObject is bool)
                return FunctionMap[typeof(bool)]("CypherBool", sourceObject);

            if (sourceObject is long)
                return FunctionMap[typeof(long)]("CypherInt", sourceObject);

            if (sourceObject is double)
                return FunctionMap[typeof(double)]("CypherFloat", sourceObject);

            if (sourceObject is string)
                return FunctionMap[typeof(string)]("CypherString", sourceObject);

            if (sourceObject is byte[])
                return FunctionMap[typeof(byte[])]("CypherByteArray", sourceObject);

            if (sourceObject as LocalDate != null)
                return FunctionMap[typeof(LocalDate)]("CypherDate", sourceObject);

            if (sourceObject as OffsetTime != null)
                return FunctionMap[typeof(OffsetTime)]("CypherTime", sourceObject);

            if (sourceObject as LocalTime != null)
                return FunctionMap[typeof(LocalTime)]("CypherLocalTime", sourceObject);

            if (sourceObject as ZonedDateTime != null)
                return FunctionMap[typeof(ZonedDateTime)]("CypherDateTime", sourceObject);

            if (sourceObject as LocalDateTime != null)
                return FunctionMap[typeof(LocalDateTime)]("CypherLocalDataTime", sourceObject);

            if (sourceObject as Duration != null)
                return FunctionMap[typeof(Duration)]("CypherDuration", sourceObject);

            if (sourceObject as Point != null)
                return FunctionMap[typeof(Point)]("CypherPoint", sourceObject);
           
            if (sourceObject as INode != null)
                return FunctionMap[typeof(INode)]("CypherNode", sourceObject);

            if (sourceObject as IRelationship != null)
                return FunctionMap[typeof(IRelationship)]("CypherRelationship", sourceObject);

            if (sourceObject as IPath != null)
                return FunctionMap[typeof(IPath)]("CypherPath", sourceObject);
            

            throw new IOException($"Attempting to convert an unsuported object type to a CypherType: {sourceObject.GetType()}");
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
            var node = (INode)obj;
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



