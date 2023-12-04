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
using FluentAssertions;
using Neo4j.Driver.Preview.Mapping;
using Xunit;

using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping;

public class BuiltMapperTests
{
    private class NoParameterlessConstructor
    {
        public int Value { get; }

        public NoParameterlessConstructor(int value)
        {
            Value = value;
        }
    }

    [Fact]
    public void ShouldThrowIfNoParameterlessConstructor()
    {
        var mapper = new BuiltMapper<NoParameterlessConstructor>();
        var act = () => mapper.Map(null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShouldUseConstructorWhenInstructed()
    {
        var mapper = new BuiltMapper<NoParameterlessConstructor>();

        var constructor = typeof(NoParameterlessConstructor).GetConstructors()[0];
        mapper.AddConstructorMapping(constructor);
        var result = mapper.Map(new Record(new[] { "value" }, new object[] { 48 }));
        result.Value.Should().Be(48);
    }
}
