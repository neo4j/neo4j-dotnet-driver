// Copyright (c) "Neo4j"
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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.Util
{
    public class ConfigBuildersTests
    {
        public class BuildTransactionOptions
        {
            [Fact]
            public void ShouldReturnEmptyTxOptionsWhenBuilderIsNull()
            {
                var options = ConfigBuilders.BuildTransactionConfig(null);
                options.Should().Be(TransactionConfig.Default);
            }

            [Fact]
            public void ShouldReturnNewTxOptions()
            {
                var options1 = ConfigBuilders.BuildTransactionConfig(o => o.WithTimeout(TimeSpan.FromSeconds(5)));
                var options2 = ConfigBuilders.BuildTransactionConfig(o => o.WithTimeout(TimeSpan.FromSeconds(30)));
                options1.Timeout.Should().Be(TimeSpan.FromSeconds(5));
                options2.Timeout.Should().Be(TimeSpan.FromSeconds(30));

                // When I reset to another value
                options1.Timeout = TimeSpan.FromMinutes(1);
                options1.Timeout.Should().Be(TimeSpan.FromMinutes(1));
                options2.Timeout.Should().Be(TimeSpan.FromSeconds(30));
            }
        }

        public class BuildSessionOptions
        {
            [Fact]
            public void ShouldReturnEmptySessionOptionsWhenBuilderIsNull()
            {
                var options = ConfigBuilders.BuildSessionConfig(null);
                options.Should().Be(SessionConfig.Default);
            }

            [Fact]
            public void ShouldReturnNewSessionOptions()
            {
                var options1 = ConfigBuilders.BuildSessionConfig(o => o.WithDatabase("neo4j"));
                var options2 = ConfigBuilders.BuildSessionConfig(o => o.WithDatabase("system"));
                options1.Database.Should().Be("neo4j");
                options2.Database.Should().Be("system");

                // When I reset to another value
                options1.Database = "foo";
                options1.Database.Should().Be("foo");
                options2.Database.Should().Be("system");
            }
        }
    }
}
