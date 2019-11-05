// Copyright (c) 2002-2019 "Neo4j,"
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
using FluentAssertions;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class TransactionOptionsTests
    {
        public class TimeoutField
        {
            public static IEnumerable<object[]> InvalidTimeSpanValues => new[]
            {
                new object[] {TimeSpan.Zero},
                new object[] {TimeSpan.FromSeconds(-1)},
                new object[] {TimeSpan.FromHours(-2)}
            };
            
            [Fact]
            public void ShouldReturnDefaultValueTimeSpanZero()
            {
                var config = new TransactionOptions();
                config.Timeout.Should().Be(TimeSpan.Zero);
                (config.Timeout == TimeSpan.Zero).Should().BeTrue();
            }
            
            [Fact]
            public void ShouldAllowToSetToNewValue()
            {
                var config = new TransactionOptions();
                config.WithTimeout(TimeSpan.FromSeconds(1));
                config.Timeout.Should().Be(TimeSpan.FromSeconds(1));
                (config.Timeout == TimeSpan.FromSeconds(1)).Should().BeTrue();
            }

            [Theory, MemberData(nameof(InvalidTimeSpanValues))]
            public void ShouldThrowExceptionIfAssigningValueZero(TimeSpan input)
            {
                var config = new TransactionOptions();
                var error = Record.Exception(()=>config.WithTimeout(input));
                error.Should().BeOfType<ArgumentOutOfRangeException>();
                error.Message.Should().Contain("not be zero or negative");
            }
        }

        public class MetadataField
        {
            [Fact]
            public void ShouldReturnDefaultValueEmptyDictionary()
            {
                var config = new TransactionOptions();
                config.Metadata.Should().BeEmpty();
            }
            
            [Fact]
            public void ShouldAllowToSetToNewValue()
            {
                var config = new TransactionOptions();
                config.WithMetadata(new Dictionary<string, object> {{"key", "value"}});
                config.Metadata.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("key", "value"));
            }

            [Fact]
            public void ShouldThrowExceptionIfAssigningNull()
            {
                var config = new TransactionOptions();
                var error = Record.Exception(() => config.WithMetadata(null));
                error.Should().BeOfType<ArgumentNullException>();
                error.Message.Should().Contain("should not be null");
            }
        }
    }
}