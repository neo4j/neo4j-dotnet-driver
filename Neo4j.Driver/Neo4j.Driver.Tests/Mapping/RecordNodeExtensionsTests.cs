﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping
{
    public class RecordNodeExtensionsTests
    {
        [Fact]
        public void ShouldGetTypedValueFromRecord_string()
        {
            var record = new Record(new[] { "key" }, new object[] { "value" });
            record.GetValue<string>("key").Should().Be("value");
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_int()
        {
            var record = new Record(new[] { "key" }, new object[] { 1L });
            record.GetValue<int>("key").Should().Be(1);
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_long()
        {
            var record = new Record(new[] { "key" }, new object[] { 1L });
            record.GetValue<long>("key").Should().Be(1L);
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_double()
        {
            var record = new Record(new[] { "key" }, new object[] { 1.0 });
            record.GetValue<double>("key").Should().Be(1.0);
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_float()
        {
            var record = new Record(new[] { "key" }, new object[] { 1.0 });
            record.GetValue<float>("key").Should().Be(1.0f);
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_bool()
        {
            var record = new Record(new[] { "key" }, new object[] { true });
            record.GetValue<bool>("key").Should().Be(true);
        }

        [Fact]
        public void ShouldGetTypedValueFromRecord_node()
        {
            var node = new Node(1, new[] { "key" }, new Dictionary<string, object> { { "key", "value" } });
            var record = new Record(new[] { "key" }, new object[] { node });
            record.GetNode("key").Should().Be(node);
        }

        [Fact]
        public void ShouldGetValueFromNode_string()
        {
            var node = new Node(1, new[] { "key" }, new Dictionary<string, object> { { "key", "value" } });
            node.GetValue<string>("key").Should().Be("value");
        }

        [Fact]
        public void ShouldGetValueFromNode_int()
        {
            var node = new Node(1, new[] { "key" }, new Dictionary<string, object> { { "key", 1L } });
            node.GetValue<int>("key").Should().Be(1);
        }

        [Fact]
        public void ShouldGetValueFromNode_Dictionary()
        {
            var dictionary = new Dictionary<string, object> { { "key", "value" } };
            var node = new Node(1, new[] { "field" }, new Dictionary<string, object> { { "field", dictionary } });
            node.GetValue<Dictionary<string, object>>("field").Should().BeEquivalentTo(dictionary);
        }
    }
}