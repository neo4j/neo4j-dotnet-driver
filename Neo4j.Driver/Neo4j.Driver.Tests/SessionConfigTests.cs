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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SessionConfigTests
    {
        [Theory]
        [InlineData((string) null)]
        [InlineData("")]
        public void ShouldThrowExceptionForInvalidDatabaseOnSetter(string name)
        {
            this.Invoking(_ => new SessionConfig().Database = name).Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData((string) null)]
        [InlineData("")]
        public void ShouldThrowExceptionForInvalidDatabaseOnBuilder(string name)
        {
            this.Invoking(_ => SessionConfig.Builder.WithDatabase(name)).Should().Throw<ArgumentNullException>();
        }
    }
}