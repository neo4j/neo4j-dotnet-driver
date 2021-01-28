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
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class ServerVersionTests
    {
        [Theory]
        [InlineData("3.2")]
        [InlineData("3.2-alpha01")]
        [InlineData("3.2.0-alpha01")]
        [InlineData("Neo4j/3.2")]
        public void ShouldHandleMajorMinorVersion(string version)
        {
            var serverVersion = ServerVersion.From(version);
            serverVersion.Major.Should().Be(3);
            serverVersion.Minor.Should().Be(2);
            serverVersion.Patch.Should().Be(0);
        }

        [Theory]
        [InlineData("3.2.1")]
        [InlineData("Neo4j/3.2.1")]
        public void ShouldHandleMajorMinorPatchVersion(string version)
        {
            var serverVersion = ServerVersion.From(version);
            serverVersion.Major.Should().Be(3);
            serverVersion.Minor.Should().Be(2);
            serverVersion.Patch.Should().Be(1);
        }

        [Fact]
        public void ShouldHandleDevVersion()
        {
            var version = "Neo4j/dev";
            var serverVersion = ServerVersion.From(version);
            serverVersion.Major.Should().Be(Int32.MaxValue);
            serverVersion.Minor.Should().Be(Int32.MaxValue);
            serverVersion.Patch.Should().Be(Int32.MaxValue);
        }

        [Theory]
        [InlineData("Neo4j/illegal")]
        [InlineData("Neo4j/3-alpha2")]
        [InlineData("Illegal")]
        [InlineData("\t\r\n")]
        public void ShouldThrowWhenVersionNotRecognized(string version)
        {
            var exc = Record.Exception(() => ServerVersion.From(version));

            exc.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ShouldThrowWhenVersionIsNullOrEmpty(string version)
        {
            var exc = Record.Exception(() => ServerVersion.From(version));

            exc.Should().BeOfType<ArgumentNullException>();
        }
        
    }
}