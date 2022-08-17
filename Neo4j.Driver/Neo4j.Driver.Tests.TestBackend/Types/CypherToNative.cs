using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal class CypherToNative
{
    //Mapping of object type to a cypher type name string that will be used in the JSON.
    private static Dictionary<string, (Type, Func<Type, CypherToNativeObject, object>)> TypeMap { get; set; } =
        new()
        {
            {"CypherList", (typeof(List<object>), CypherList)},
            {"CypherMap", (typeof(Dictionary<string, object>), CypherMap)},
            {"CypherBool", (typeof(bool), CypherSimple)},
            {"CypherInt", (typeof(long), CypherSimple)},
            {"CypherFloat", (typeof(double), CypherSimple)},
            {"CypherString", (typeof(string), CypherSimple)},
            {"CypherByteArray", (typeof(byte[]), CypherSimple)},
            {"CypherDate", (typeof(LocalDate), CypherDateTime)},
            {"CypherTime", (typeof(OffsetTime), CypherDateTime)},
            {"CypherLocalTime", (typeof(LocalTime), CypherDateTime)},
            {"CypherDateTime", (typeof(ZonedDateTime), CypherDateTime)},
            {"CypherLocalDateTime", (typeof(LocalDateTime), CypherDateTime)},
            {"CypherDuration", (typeof(Duration), CypherDuration)},
            {"CypherPoint", (typeof(Point), CypherTODO)},
            {"CypherNode", (typeof(INode), CypherTODO)},
            {"CypherRelationship", (typeof(IRelationship), CypherTODO)},
            {"CypherPath", (typeof(IPath), CypherTODO)}
        };

    public static object Convert(CypherToNativeObject sourceObject)
    {
        if (sourceObject.name == "CypherNull")
            return null;

        if (TypeMap.TryGetValue(sourceObject.name, out var mapper))
            return mapper.Item2(mapper.Item1, sourceObject);

        throw new IOException(
            $"Attempting to convert an unsuported object type to a CypherType: {sourceObject.GetType()}");
    }

    public static object CypherSimple(Type objectType, CypherToNativeObject cypherObject)
    {
        return ((SimpleValue) cypherObject.data).value;
    }

    public static object CypherTODO(Type objectType, CypherToNativeObject cypherObject)
    {
        throw new NotImplementedException(
            $"CypherToNative : {cypherObject.name} conversion is not implemented yet");
    }

    public static object CypherList(Type objectType, CypherToNativeObject obj)
    {
        var result = new List<object>();

        foreach (JObject item in (JArray) ((SimpleValue) obj.data).value)
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