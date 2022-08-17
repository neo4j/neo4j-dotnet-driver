// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal static class NativeToCypher
{
    //Mapping of object type to a conversion delegate that will return a NativeToCypherObject that can be serialized to JSON.
    private static readonly Dictionary<Type, (string, Func<string, object, NativeToCypherObject>)> _functionMap = new()
    {
        {typeof(List<object>), ("CypherList", CypherList)},
        {typeof(Dictionary<string, object>), ("CypherMap", CypherMap)},
        {typeof(bool), ("CypherBool", CypherSimple)},
        {typeof(long), ("CypherInt", CypherSimple)},
        {typeof(double), ("CypherFloat", CypherSimple)},
        {typeof(string), ("CypherString", CypherSimple)},
        {typeof(byte[]), ("CypherByteArray", CypherSimple)},
        {typeof(LocalDate), ("CypherDate", CypherDateTime)},
        {typeof(OffsetTime), ("CypherTime", CypherDateTime)},
        {typeof(LocalTime), ("CypherTime", CypherDateTime)},
        {typeof(ZonedDateTime), ("CypherDateTime", CypherDateTime)},
        {typeof(LocalDateTime), ("CypherDateTime", CypherDateTime)},
        {typeof(Duration), ("CypherDuration", CypherDuration)},
        {typeof(Point), ("CypherPoint", CypherTODO)},
        {typeof(INode), ("CypherNode", CypherNode)},
        {typeof(IRelationship), ("CypherRelationship", CypherRelationship)},
        {typeof(IPath), ("CypherPath", CypherPath)}
    };

    public static object Convert(object sourceObject)
    {
        if (sourceObject == null)
            return new NativeToCypherObject {name = "CypherNull"};

        if (_functionMap.TryGetValue(sourceObject.GetType(), out var mapper))
            return mapper.Item2(mapper.Item1, sourceObject);

        throw new IOException(
            $"Attempting to convert an unsupported object type to a CypherType: {sourceObject.GetType()}");
    }

    public static NativeToCypherObject CypherSimple(string cypherType, object obj)
    {
        return new NativeToCypherObject {name = cypherType, data = new NativeToCypherObject.DataType {value = obj}};
    }

    public static NativeToCypherObject CypherMap(string cypherType, object obj)
    {
        var result = new Dictionary<string, object>();

        foreach (var pair in (Dictionary<string, object>) obj) result[pair.Key] = Convert(pair.Value);

        return new NativeToCypherObject {name = cypherType, data = new NativeToCypherObject.DataType {value = result}};
    }

    public static NativeToCypherObject CypherList(string cypherType, object obj)
    {
        var result = new List<object>();

        foreach (var item in (List<object>) obj) result.Add(Convert(item));

        return new NativeToCypherObject {name = cypherType, data = new NativeToCypherObject.DataType {value = result}};
    }

    public static NativeToCypherObject CypherNode(string cypherType, object obj)
    {
        var node = (INode) obj;

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

        return new NativeToCypherObject {name = "Node", data = cypherNode};
    }

    public static NativeToCypherObject CypherRelationship(string cypherType, object obj)
    {
        var rel = (IRelationship) obj;
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

        return new NativeToCypherObject {name = "Relationship", data = cypherRel};
    }

    public static NativeToCypherObject CypherPath(string cypherType, object obj)
    {
        var path = (IPath) obj;
        var cypherPath = new Dictionary<string, object>
        {
            ["nodes"] = Convert(path.Nodes.OfType<object>().ToList()),
            ["relationships"] = Convert(path.Relationships.OfType<object>().ToList())
        };

        return new NativeToCypherObject {name = "Path", data = cypherPath};
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

    public static NativeToCypherObject CypherTODO(string name, object obj)
    {
        throw new NotImplementedException($"NativeToCypher : {name} conversion is not implemented yet");
    }
}