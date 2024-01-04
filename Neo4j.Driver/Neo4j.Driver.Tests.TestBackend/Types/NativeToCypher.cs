﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable CS0618 // Type or member is obsolete - but still needs to be handled

namespace Neo4j.Driver.Tests.TestBackend;

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
    private static Dictionary<Type, Func<string, object, NativeToCypherObject>> FunctionMap { get; } = new()
    {
        { typeof(List<object>), CypherList },
        { typeof(Dictionary<string, object>), CypherMap },

        { typeof(bool), CypherSimple },
        { typeof(long), CypherSimple },
        { typeof(double), CypherSimple },
        { typeof(string), CypherSimple },
        { typeof(byte[]), CypherSimple },

        { typeof(LocalDate), CypherDateTime },
        { typeof(OffsetTime), CypherDateTime },
        { typeof(LocalTime), CypherDateTime },
        { typeof(ZonedDateTime), CypherDateTime },
        { typeof(LocalDateTime), CypherDateTime },
        { typeof(Duration), CypherDuration },
        { typeof(Point), CypherTODO },

        { typeof(INode), CypherNode },
        { typeof(IRelationship), CypherRelationship },
        { typeof(IPath), CypherPath }
    };

    public static object Convert(object sourceObject)
    {
        if (sourceObject is null)
        {
            return new NativeToCypherObject { name = "CypherNull" };
        }

        if (sourceObject as List<object> != null)
        {
            return FunctionMap[typeof(List<object>)]("CypherList", sourceObject);
        }

        if (sourceObject as Dictionary<string, object> != null)
        {
            return FunctionMap[typeof(Dictionary<string, object>)]("CypherMap", sourceObject);
        }

        if (sourceObject is bool)
        {
            return FunctionMap[typeof(bool)]("CypherBool", sourceObject);
        }

        if (sourceObject is long)
        {
            return FunctionMap[typeof(long)]("CypherInt", sourceObject);
        }

        if (sourceObject is double)
        {
            return FunctionMap[typeof(double)]("CypherFloat", sourceObject);
        }

        if (sourceObject is string)
        {
            return FunctionMap[typeof(string)]("CypherString", sourceObject);
        }

        if (sourceObject is byte[])
        {
            return FunctionMap[typeof(byte[])]("CypherByteArray", sourceObject);
        }

        if (sourceObject as LocalDate != null)
        {
            return FunctionMap[typeof(LocalDate)]("CypherDate", sourceObject);
        }

        if (sourceObject as OffsetTime != null)
        {
            return FunctionMap[typeof(OffsetTime)]("CypherTime", sourceObject);
        }

        if (sourceObject as LocalTime != null)
        {
            return FunctionMap[typeof(LocalTime)]("CypherTime", sourceObject);
        }

        if (sourceObject as ZonedDateTime != null)
        {
            return FunctionMap[typeof(ZonedDateTime)]("CypherDateTime", sourceObject);
        }

        if (sourceObject as LocalDateTime != null)
        {
            return FunctionMap[typeof(LocalDateTime)]("CypherDateTime", sourceObject);
        }

        if (sourceObject as Duration != null)
        {
            return FunctionMap[typeof(Duration)]("CypherDuration", sourceObject);
        }

        if (sourceObject as Point != null)
        {
            return FunctionMap[typeof(Point)]("CypherPoint", sourceObject);
        }

        if (sourceObject as INode != null)
        {
            return FunctionMap[typeof(INode)]("CypherNode", sourceObject);
        }

        if (sourceObject as IRelationship != null)
        {
            return FunctionMap[typeof(IRelationship)]("CypherRelationship", sourceObject);
        }

        if (sourceObject as IPath != null)
        {
            return FunctionMap[typeof(IPath)]("CypherPath", sourceObject);
        }

        throw new IOException(
            $"Attempting to convert an unsupported object type to a CypherType: {sourceObject.GetType()}");
    }

    public static NativeToCypherObject CypherSimple(string cypherType, object obj)
    {
        return new NativeToCypherObject { name = cypherType, data = new NativeToCypherObject.DataType { value = obj } };
    }

    public static NativeToCypherObject CypherMap(string cypherType, object obj)
    {
        var result = new Dictionary<string, object>();

        foreach (var pair in (Dictionary<string, object>)obj)
        {
            result[pair.Key] = Convert(pair.Value);
        }

        return new NativeToCypherObject
            { name = cypherType, data = new NativeToCypherObject.DataType { value = result } };
    }

    public static NativeToCypherObject CypherList(string cypherType, object obj)
    {
        var result = new List<object>();

        foreach (var item in (List<object>)obj)
        {
            result.Add(Convert(item));
        }

        return new NativeToCypherObject
            { name = cypherType, data = new NativeToCypherObject.DataType { value = result } };
    }

    public static NativeToCypherObject CypherTODO(string name, object obj)
    {
        throw new NotImplementedException($"NativeToCypher : {name} conversion is not implemented yet");
    }

    public static NativeToCypherObject CypherNode(string cypherType, object obj)
    {
        var node = (INode)obj;

        Dictionary<string, object> cypherNode;
        try
        {
            cypherNode = new Dictionary<string, object>
            {
                ["id"] = Convert(node.Id),
                ["elementId"] = Convert(node.ElementId),
                ["labels"] = Convert(new List<object>(node.Labels)),
                ["props"] = Convert(new Dictionary<string, object>(node.Properties))
            };
        }
        catch (InvalidOperationException)
        {
            cypherNode = new Dictionary<string, object>
            {
                ["id"] = Convert(-1L),
                ["elementId"] = Convert(node.ElementId),
                ["labels"] = Convert(new List<object>(node.Labels)),
                ["props"] = Convert(new Dictionary<string, object>(node.Properties))
            };
        }

        return new NativeToCypherObject { name = "Node", data = cypherNode };
    }

    public static NativeToCypherObject CypherRelationship(string cypherType, object obj)
    {
        var rel = (IRelationship)obj;
        Dictionary<string, object> cypherRel;
        try
        {
            cypherRel = new Dictionary<string, object>
            {
                ["id"] = Convert(rel.Id),
                ["startNodeId"] = Convert(rel.StartNodeId),
                ["type"] = Convert(rel.Type),
                ["endNodeId"] = Convert(rel.EndNodeId),
                ["props"] = Convert(new Dictionary<string, object>(rel.Properties)),
                ["elementId"] = Convert(rel.ElementId),
                ["startNodeElementId"] = Convert(rel.StartNodeElementId),
                ["endNodeElementId"] = Convert(rel.EndNodeElementId)
            };
        }
        catch (InvalidOperationException)
        {
            cypherRel = new Dictionary<string, object>
            {
                ["id"] = Convert(-1L),
                ["startNodeId"] = Convert(-1L),
                ["type"] = Convert(rel.Type),
                ["endNodeId"] = Convert(-1L),
                ["props"] = Convert(new Dictionary<string, object>(rel.Properties)),
                ["elementId"] = Convert(rel.ElementId),
                ["startNodeElementId"] = Convert(rel.StartNodeElementId),
                ["endNodeElementId"] = Convert(rel.EndNodeElementId)
            };
        }

        return new NativeToCypherObject { name = "Relationship", data = cypherRel };
    }

    public static NativeToCypherObject CypherPath(string cypherType, object obj)
    {
        var path = (IPath)obj;
        var cypherPath = new Dictionary<string, object>
        {
            ["nodes"] = Convert(path.Nodes.OfType<object>().ToList()),
            ["relationships"] = Convert(path.Relationships.OfType<object>().ToList())
        };

        return new NativeToCypherObject { name = "Path", data = cypherPath };
    }

    private static NativeToCypherObject CypherDateTime(string cypherType, object obj)
    {
        return obj switch
        {
            ZonedDateTime zonedDateTime => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["year"] = zonedDateTime.Year,
                    ["month"] = zonedDateTime.Month,
                    ["day"] = zonedDateTime.Day,
                    ["hour"] = zonedDateTime.Hour,
                    ["minute"] = zonedDateTime.Minute,
                    ["second"] = zonedDateTime.Second,
                    ["nanosecond"] = zonedDateTime.Nanosecond,
                    ["utc_offset_s"] = zonedDateTime.OffsetSeconds,
                    ["timezone_id"] = zonedDateTime.Zone is ZoneId zoneId ? zoneId.Id : null
                }
            },
            LocalDateTime localDateTime => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["year"] = localDateTime.Year,
                    ["month"] = localDateTime.Month,
                    ["day"] = localDateTime.Day,
                    ["hour"] = localDateTime.Hour,
                    ["minute"] = localDateTime.Minute,
                    ["second"] = localDateTime.Second,
                    ["nanosecond"] = localDateTime.Nanosecond
                }
            },
            LocalDate localDate => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["year"] = localDate.Year,
                    ["month"] = localDate.Month,
                    ["day"] = localDate.Day
                }
            },
            OffsetTime offsetTime => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["hour"] = offsetTime.Hour,
                    ["minute"] = offsetTime.Minute,
                    ["second"] = offsetTime.Second,
                    ["nanosecond"] = offsetTime.Nanosecond,
                    ["utc_offset_s"] = offsetTime.OffsetSeconds
                }
            },
            LocalTime localTime => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["hour"] = localTime.Hour,
                    ["minute"] = localTime.Minute,
                    ["second"] = localTime.Second,
                    ["nanosecond"] = localTime.Nanosecond
                }
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static NativeToCypherObject CypherDuration(string cypherType, object obj)
    {
        return obj switch
        {
            Duration duration => new NativeToCypherObject
            {
                name = cypherType,
                data = new Dictionary<string, object>
                {
                    ["months"] = duration.Months,
                    ["days"] = duration.Days,
                    ["seconds"] = duration.Seconds,
                    ["nanoseconds"] = duration.Nanos
                }
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
