// Copyright (c) "Neo4j"
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltTypesIT: DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public BoltTypesIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {}

        [RequireServerFact]
        public void ShouldEchoVeryLongString()
        {
            string input = new string('*', 10000);
            VerifyCanEcho(input);
        }

        [RequireServerTheory]
        [InlineData(null)]
        [InlineData(1L)]
        [InlineData(1.1d)]
        [InlineData("hello")]
        [InlineData(true)]
        public void ShouldEchoVeryLongList(object item)
        {
            var input = new List<object>();
            for (int i = 0; i < 1000; i++)
            {
                input.Add(item);
            }
            VerifyCanEcho(input);
        }

        [RequireServerTheory]
        [InlineData(null)]
        [InlineData(1L)]
        [InlineData(1.1d)]
        [InlineData("hello")]
        [InlineData(true)]
        public void ShouldEchoVeryLongMap(object item)
        {
            var input = new Dictionary<string, object>();
            for (int i = 0; i < 1000; i++)
            {
                input.Add(i.ToString(), item);
            }
            VerifyCanEcho(input);
        }

        [RequireServerTheory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(1L)]
        [InlineData(-17L)]
        [InlineData(-129L)]
        [InlineData(129L)]
        [InlineData(2147483647L)]
        [InlineData(-2147483648L)]
        [InlineData(9223372036854775807L)]
        [InlineData(-9223372036854775808L)]
        [InlineData(1.7976931348623157E+308d)]
        [InlineData(2.2250738585072014e-308d)]
        [InlineData(4.9E-324d)]
        [InlineData(0.0d)]
        [InlineData(1.1d)]
        [InlineData("1")]
        [InlineData("-17∂ßå®")]
        [InlineData("String")]
        [InlineData("")]
        public void ShouldEchoBack(object item)
        {
            VerifyCanEcho(item);
        }

        [RequireServerFact]
        public void ShouldEchoListAndNestedList()
        {
            var listOfItems = new List<object>
            {
                new List<object> {1L, 2L, 3L, 4L},
                new List<object> {true, false},
                new List<object> {1.1, 2.2, 3.3},
                new List<object> {"a", "b", "c", "˚C"},
                new List<object> {null, null},
                new List<object> {null, true, "-17∂ßå®", 1.7976931348623157E+308d, -9223372036854775808d}
            };

            foreach (var item in listOfItems)
            {
                VerifyCanEcho(item);
            }

            VerifyCanEcho(listOfItems);
        }

        [RequireServerFact]
        public void ShouldEchoMapAndNestedMap()
        {
            var dictOfDict = new Dictionary<string, object>
            {
                {"a", new Dictionary<string, object> {{"a", 1L}, {"b", 2L}, {"c", 3L}, {"d", 4L}}},
                {"b", new Dictionary<string, object> {{"a", true}, {"b", false}}},
                {"c", new Dictionary<string, object> {{"a", 1.1}, {"b", 2.2}, {"c", 3.3}}},
                {"d", new Dictionary<string, object> {{"a", "c"}, {"b", "d"}, {"c", "e"}, {"e", "˚C"}}},
                {"e", new Dictionary<string, object> {{"a", null}}},
                {"f", new Dictionary<string, object> {{"a", 1L}, {"b", true}, {"c", 1.1}, {"d", "˚C"}, {"e", null}}},
            };

            foreach (var item in dictOfDict.Values)
            {
                VerifyCanEcho(item);
            }

            VerifyCanEcho(dictOfDict);
        }

        private void VerifyCanEcho(object input)
        {
            using (var session = Driver.Session())
            {
                var record = session.Run("RETURN $x as y", new Dictionary<string, object> {{"x", input}}).Single();
                AssertEqual(record["y"], input);
            }
        }

        private static void AssertEqual(object value, object other)
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
    }
}