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
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class TransactionConfigTests
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
                var config = new TransactionConfig();
                config.Timeout.Should().Be(TimeSpan.Zero);
                (config.Timeout == TimeSpan.Zero).Should().BeTrue();
            }
            
            [Fact]
            public void ShouldAllowToSetToNewValue()
            {
                var config = new TransactionConfig();
                config.Timeout = TimeSpan.FromSeconds(1);
                config.Timeout.Should().Be(TimeSpan.FromSeconds(1));
                (config.Timeout == TimeSpan.FromSeconds(1)).Should().BeTrue();
            }

            [Theory, MemberData(nameof(InvalidTimeSpanValues))]
            public void ShouldThrowExceptionIfAssigningValueZero(TimeSpan input)
            {
                var config = new TransactionConfig();
                var error = Record.Exception(()=>config.Timeout = input);
                error.Should().BeOfType<ArgumentOutOfRangeException>();
                error.Message.Should().Contain("not be zero or negative");
            }
        }

        public class MetadataField
        {
            [Fact]
            public void ShouldReturnDefaultValueEmptyDictionary()
            {
                var config = new TransactionConfig();
                config.Metadata.Should().BeEmpty();
            }
            
            [Fact]
            public void ShouldAllowToSetToNewValue()
            {
                var config = new TransactionConfig();
                config.Metadata = new Dictionary<string, object>{{"key", "value"}};
                config.Metadata.Should().HaveCount(1).And.Contain(new KeyValuePair<string, object>("key", "value"));
            }

            [Fact]
            public void ShouldThrowExceptionIfAssigningNull()
            {
                var config = new TransactionConfig();
                var error = Record.Exception(() => config.Metadata = null);
                error.Should().BeOfType<ArgumentNullException>();
                error.Message.Should().Contain("should not be null");
            }
        }

        public class IsEmptyMethod
        {
            [Fact]
            public void DefaultValueShouldBeEmpty()
            {
                var config = new TransactionConfig();
                config.IsEmpty().Should().BeTrue();
            }

            [Fact]
            public void EmptyConfigShouldBeEmpty()
            {
                TransactionConfig.Empty.IsEmpty().Should().BeTrue();
            }
        }

        public class EqualsMethod
        {
            [Fact]
            public void NewConfigShouldEqualsToEmpty()
            {
                new TransactionConfig().Equals(TransactionConfig.Empty).Should().BeTrue();
                TransactionConfig.Empty.Equals(new TransactionConfig()).Should().BeTrue();
                new TransactionConfig().Equals((object)TransactionConfig.Empty).Should().BeTrue();
                TransactionConfig.Empty.Equals((object)new TransactionConfig()).Should().BeTrue();
            }

            [Fact]
            public void ConfigWithSameValueShouldBeEqualsToEachOther()
            {
                var config1 = new TransactionConfig
                {
                    Metadata = new Dictionary<string, object> {{"Molly", "MostlyWhite"}},
                    Timeout = TimeSpan.FromMinutes(6)
                };
                
                var config2 = new TransactionConfig
                {
                    Timeout = TimeSpan.FromMinutes(6),
                    Metadata = new Dictionary<string, object> {{"Molly", "MostlyWhite"}}
                };

                config1.Equals(config2).Should().BeTrue();
                config2.Equals(config1).Should().BeTrue();
            }
            
            [Fact]
            public void ShouldNotEqualToNull()
            {
                var config2 = new TransactionConfig
                {
                    Timeout = TimeSpan.FromMinutes(6),
                    Metadata = new Dictionary<string, object> {{"Molly", "MostlyWhite"}}
                };

                config2.Equals(null).Should().BeFalse();
            }
        }
    }
}