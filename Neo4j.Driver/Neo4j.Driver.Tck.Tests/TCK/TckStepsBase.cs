// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using static Neo4j.Driver.Tck.Tests.TCK.CypherRecordParser;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    public class TckStepsBase
    {
        public const string Url = "bolt://localhost:7687";
        protected static IDriver Driver;
        protected static INeo4jInstaller Installer;

        protected static object GetValue(string type, string value)
        {
            switch (type)
            {
                case "Null":
                    return null;
                case "Boolean":
                    return Convert.ToBoolean(value);
                case "Integer":
                    return Convert.ToInt64(value);
                case "Float":
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                case "String":
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type {type}");
            }
        }

        protected static void AssertEqual(object value, object other)
        {
            if (value == null || value is bool || value is long || value is double || value is string)
            {
                Assert.Equal(value, other);
            }
            else if (value is IList)
            {
                var valueList = (IList)value;
                var otherList = (IList)other;
                AssertEqual(valueList.Count, otherList.Count);
                for (var i = 0; i < valueList.Count; i++)
                {
                    AssertEqual(valueList[i], otherList[i]);
                }
            }
            else if (value is IDictionary)
            {
                var valueDic = (IDictionary<string, object>)value;
                var otherDic = (IDictionary<string, object>)other;
                AssertEqual(valueDic.Count, otherDic.Count);
                foreach (var key in valueDic.Keys)
                {
                    otherDic.ContainsKey(key).Should().BeTrue();
                    AssertEqual(valueDic[key], otherDic[key]);
                }
            }
        }

        protected static void DisposeDriver()
        {
            Driver?.Dispose();
        }

        protected static void CreateNewDriver()
        {
            Driver = GraphDatabase.Driver(Url);
        }

        protected static void AssertRecordsAreTheSame(List<IRecord> actual, List<IRecord> expected)
        {
            actual.Should().HaveSameCount(expected);
            foreach (var aRecord in actual)
            {
                AssertionExtensions.Should((bool) AssertContains(expected, aRecord)).BeTrue();
            }
        }

        protected static bool AssertContains(List<IRecord> records, IRecord aRecord)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var record in records)
            {
                if (RecordEquals(record, aRecord))
                {
                    return true;
                }
            }
            return false;
        }

        protected static bool RecordEquals(IRecord r1, IRecord r2)
        {
            return r1.Keys.SequenceEqual(r2.Keys) && r1.Keys.All(key => CypherValueEquals(r1[key], r2[key]));
        }

        protected static bool CypherValueEquals(object o1, object o2)
        {
            // long/double/bool/null/string/list<object>/dict<string, object>/path/node/rel
            if (ReferenceEquals(o1, o2)) return true;
            if (o1.GetType() != o2.GetType()) return false;
            if (o1 is string)
            {
                return (string)o1 == (string)o2;
            }
            if (o1 is IDictionary<string, object>)
            {
                var dict = (IDictionary<string, object>)o1;
                var dict2 = (IDictionary<string, object>)o2;
                return dict.Keys.SequenceEqual(dict2.Keys) && dict.Keys.All(key => CypherValueEquals(dict[key], dict2[key]));
            }
            if (o1 is IList<object>)
            {
                var list1 = (IList<object>)o1;
                var list2 = (IList<object>)o2;
                if (list1.Count != list2.Count)
                {
                    return false;
                }
                return !list1.Where((t, i) => !CypherValueEquals(t, list2[i])).Any();
            }
            if (o1 is INode)
            {
                return NodeToString((INode)o1) == NodeToString((INode)o2);
            }
            if (o1 is IRelationship)
            {
                return RelToString((IRelationship)o1) == RelToString((IRelationship)o2);
            }
            if (o1 is IPath)
            {
                return PathToString((IPath)o1) == PathToString((IPath)o2);
            }
            return Equals(o1, o2);
        }
    }
}