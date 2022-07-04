using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{    
    internal class CypherToNativeObject
    {
        public string name { get; set; }
        public object data { get; set; }
    }

    internal class SimpleValue
    {
        public object? value { get; set; }
    }

    public class DateTimeParameterValue
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? day { get; set; }
        public int? hour { get; set; }
        public int? minute { get; set; }
        public int? second { get; set; }
        public int? nanosecond { get; set; }
        public int? utc_offset_s { get; set; }
        public string timezone_id { get; set; }
    }

    public class DurationParameterValue
    {
        public long? months { get; set; }
        public long? days { get; set; }
        public long? seconds { get; set; }
        public int? nanoseconds { get; set; }
    }

    internal class CypherToNative
    {
        //Mapping of object type to a cypher type name string that will be used in the JSON.
        private static Dictionary<string, Type> TypeMap { get; set; } = new Dictionary<string, Type>()
        {
            {"CypherList",               typeof(List<object>)                    },
            {"CypherMap",                typeof(Dictionary<string, object>)      },

            {"CypherBool",               typeof(bool)                            },
            {"CypherInt",                typeof(long)                            },
            {"CypherFloat",              typeof(double)                          },
            {"CypherString",             typeof(string)                          },
            {"CypherByteArray",          typeof(byte[])                          },

            {"CypherDate",               typeof(LocalDate)                       },
            {"CypherTime",               typeof(OffsetTime)                      },
            {"CypherLocalTime",          typeof(LocalTime)                       },
            {"CypherDateTime",           typeof(ZonedDateTime)                   },
            {"CypherLocalDateTime",      typeof(LocalDateTime)                   },
            {"CypherDuration",           typeof(Duration)                        },
            {"CypherPoint",              typeof(Point)                           },

            {"CypherNode",               typeof(INode)                            },
            {"CypherRelationship",       typeof(IRelationship)                    },
            {"CypherPath",               typeof(IPath)}
        };

        //Mapping of object type to a conversion delegate that will return a NativeToCypherObject that can be serialized to JSON.
        private static Dictionary<Type, Func<Type, CypherToNativeObject, object>> FunctionMap { get; set; } = new Dictionary<Type, Func<Type, CypherToNativeObject, object>>()
        {
            { typeof(List<object>),                     CypherList },
            { typeof(Dictionary<string, object>),       CypherMap },

            { typeof(bool),                             CypherSimple },
            { typeof(long),                             CypherSimple },
            { typeof(double),                           CypherSimple },
            { typeof(string),                           CypherSimple },
            { typeof(byte[]),                           CypherSimple },

            { typeof(LocalDate),                        CypherDateTime },
            { typeof(OffsetTime),                       CypherDateTime },
            { typeof(LocalTime),                        CypherDateTime },
            { typeof(ZonedDateTime),                    CypherDateTime },
            { typeof(LocalDateTime),                    CypherDateTime },
            { typeof(Duration),                         CypherDuration },
            { typeof(Point),                            CypherTODO },

            { typeof(INode),                             CypherTODO },
            { typeof(IRelationship),                     CypherTODO },
            { typeof(IPath), CypherTODO }
        };


        public static object Convert(CypherToNativeObject sourceObject)
        {
            if (sourceObject.name == "CypherNull")
            {
                return null;
            }

            try
            {
                Type objectType = TypeMap[sourceObject.name];
                var function = FunctionMap[objectType];

                return function(objectType, sourceObject);
            }
            catch
            {
                throw new IOException($"Attempting to convert an unsuported object type to a CypherType: {sourceObject.GetType()}");
            }
        }
        

        public static object CypherSimple(Type objectType, CypherToNativeObject cypherObject)
        {
            return ((SimpleValue)cypherObject.data).value;
        }
        
        public static object CypherTODO(Type objectType, CypherToNativeObject cypherObject)
        {
            throw new NotImplementedException($"CypherToNative : {cypherObject.name} conversion is not implemented yet");
        }

        public static object CypherList(Type objectType, CypherToNativeObject obj)
        {
            var result = new List<object>();

            foreach(JObject item in (JArray)((SimpleValue)obj.data).value)
            {
                result.Add(Convert(JsonCypherParameterParser.ExtractParameterFromProperty(item)));
            }

            return result;
        }

        public static object CypherMap(Type objectType, CypherToNativeObject obj)
        {
            return JObject.FromObject(((SimpleValue) obj.data).value).Properties().ToDictionary(x => x.Name, x => 
                    Convert(JsonCypherParameterParser.ExtractParameterFromProperty(x.Value as JObject)));
        }

        private static object CypherDateTime(Type objectType, CypherToNativeObject obj)
        {
            var dataTimeParam = obj.data as DateTimeParameterValue;
            
            //date & time
            if (dataTimeParam.year.HasValue && dataTimeParam.hour.HasValue)
            {
                //zoned date time
                if (dataTimeParam.utc_offset_s.HasValue || dataTimeParam.timezone_id != null)
                {
                    return new ZonedDateTime(
                        dataTimeParam.year.Value,
                        dataTimeParam.month.Value,
                        dataTimeParam.day.Value,
                        dataTimeParam.hour.Value,
                        dataTimeParam.minute.Value,
                        dataTimeParam.second.Value,
                        dataTimeParam.nanosecond.Value,
                        dataTimeParam.timezone_id != null
                            ? Zone.Of(dataTimeParam.timezone_id)
                            : Zone.Of(dataTimeParam.utc_offset_s ?? 0)
                    );
                }

                // local
                return new LocalDateTime(
                    dataTimeParam.year.Value,
                    dataTimeParam.month.Value,
                    dataTimeParam.day.Value,
                    dataTimeParam.hour.Value,
                    dataTimeParam.minute.Value,
                    dataTimeParam.second.Value,
                    dataTimeParam.nanosecond.Value
                );
            }

            if (dataTimeParam.year.HasValue)
            {
                //date local
                return new LocalDate(
                    dataTimeParam.year.Value,
                    dataTimeParam.month.Value,
                    dataTimeParam.day.Value
                );
            }

            // time offset
            if (dataTimeParam.utc_offset_s.HasValue)
            {
                return new OffsetTime(
                    dataTimeParam.hour.Value,
                    dataTimeParam.minute.Value,
                    dataTimeParam.second.Value,
                    dataTimeParam.nanosecond.Value,
                    dataTimeParam.utc_offset_s ?? 0
                );
            }

            //time
            return new LocalTime(
                dataTimeParam.hour.Value,
                dataTimeParam.minute.Value,
                dataTimeParam.second.Value,
                dataTimeParam.nanosecond.Value
            );
        }

        private static object CypherDuration(Type objectType, CypherToNativeObject obj)
        {
            var duration = obj.data as DurationParameterValue;

            return new Duration(duration.months.Value, duration.days.Value,
                duration.seconds.Value, duration.nanoseconds.Value);
        }

        /*
        public static NativeToCypherObject CypherNode(string cypherType, object obj)
        {
            var node = (Node)obj;
            var cypherNode = new Dictionary<string, object>
            {
                ["id"] = Convert(node.Id),
                ["labels"] = Convert(new List<object>(node.Labels)),
                ["props"] = Convert(new Dictionary<string, object>(node.Properties))
            };

            return new NativeToCypherObject() { name = "Node", data = cypherNode };
        }
        */
    }
}
