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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ExtensionsTests
    {
        public class GetValueMethod
        {
            [Fact]
            public void ShouldGetDefaultValueCorrectly()
            {
                var dict = new Dictionary<string, object>();
                int defaultValue = 10;
                dict.GetValue("any", defaultValue).Should().Be(defaultValue);
            }

            [Fact]
            public void ShouldGetValueCorrectlyWhenExpectingInt()
            {
                object o = 10;
                var dict = new Dictionary<string, object> { { "any", o } };
                var actual = dict.GetValue("any", -1);
                actual.Should().Be(10);
            }

            [Fact]
            public void ShouldGetDefaultValueCorrectlyWhenExpectingList()
            {
                var dict = new Dictionary<string, object>();
                List<int> defaultValue = new List<int>();
                List<int> actual = dict.GetValue("any", defaultValue);
                actual.Should().BeOfType<List<int>>();
                actual.Should().BeEmpty();
            }

            [Fact]
            public void ShouldGetValueCorrectlyWhenExpectingList()
            {
                var dict = new Dictionary<string, object> { {"any", new List<object> {11} } };
                var actual = dict.GetValue("any", new List<object>()).Cast<int>().ToList();
                actual.Should().BeOfType<List<int>>();
                actual.Should().ContainInOrder(11);
            }

            [Fact]
            public void ShouldGetDefaultValueCorrectlyWhenExpectingMap()
            {
                var dict = new Dictionary<string, object>();
                var defaultValue = new Dictionary<string, int>();
                var actual = dict.GetValue("any", defaultValue);
                actual.Should().BeOfType<Dictionary<string, int>>();
                actual.Should().BeEmpty();
            }

            [Fact]
            public void ShouldGetValueCorrectlyWhenExpectingMap()
            {
                var dict = new Dictionary<string, object>
                {
                    { "any", new Dictionary<string, object> { {"lala", 1} }}
                };
                var actual = dict.GetValue("any", new Dictionary<string, object>());
                actual.Should().BeOfType<Dictionary<string, object>>();
                actual.GetValue("lala", 0).Should().Be(1);
            }
        }
    }
}
