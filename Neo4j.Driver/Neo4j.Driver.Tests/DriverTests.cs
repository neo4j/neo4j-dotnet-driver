// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class DriverTests
    {
        [Fact]
        public void ShouldUseDefaultPortWhenPortNotSet()
        {
            using (var driver = (Internal.Driver)GraphDatabase.Driver("bolt://localhost"))
            {
                driver.Uri.Port.Should().Be(7687);
                driver.Uri.Scheme.Should().Be("bolt");
                driver.Uri.Host.Should().Be("localhost");
            }
        }

        [Fact]
        public void ShouldUseSpecifiedPortWhenPortSet()
        {
            using (var driver = (Internal.Driver)GraphDatabase.Driver("bolt://localhost:8888"))
            {
                driver.Uri.Port.Should().Be(8888);
                driver.Uri.Scheme.Should().Be("bolt");
                driver.Uri.Host.Should().Be("localhost");
            }
        }

        [Fact]
        public void ShouldSupportIPv6()
        {
            using (var driver = (Internal.Driver)GraphDatabase.Driver("bolt://[::1]"))
            {
                driver.Uri.Port.Should().Be(7687);
                driver.Uri.Scheme.Should().Be("bolt");
                driver.Uri.Host.Should().Be("[::1]");
            }
        }

        [Fact]
        public void ShouldErrorIfUriWrongFormat()
        {
            var exception = Record.Exception(() => GraphDatabase.Driver("bolt://*"));
            exception.Should().BeOfType<UriFormatException>();
        }

        [Fact]
        public void ShouldErrorIfBoltSchemeWithRoutingContext()
        {
            var exception = Record.Exception(() => GraphDatabase.Driver("bolt://localhost/?name=molly&age=1&color=white"));
            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Contain("Routing context are not supported with scheme 'bolt'");
        }

        [Fact]
        public void ShouldAcceptIfRoutingSchemeWithRoutingContext()
        {
            using (var driver = (Internal.Driver) GraphDatabase.Driver("bolt+routing://localhost/?name=molly&age=1&color=white"))
            {
                driver.Uri.Port.Should().Be(7687);
                driver.Uri.Scheme.Should().Be("bolt+routing");
                driver.Uri.Host.Should().Be("localhost");
            }
        }

        [Fact]
        public void DisposeClosesDriver()
        {
            var driver = GraphDatabase.Driver("bolt://localhost");
            driver.Dispose();

            var ex = Record.Exception(() => driver.Session());
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public void CloseClosesDriver()
        {
            var driver = GraphDatabase.Driver("bolt://localhost");
            driver.Close();

            var ex = Record.Exception(() => driver.Session());
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public async void CloseAsyncClosesDriver()
        {
            var driver = GraphDatabase.Driver("bolt://localhost");
            await driver.CloseAsync();

            var ex = Record.Exception(() => driver.Session());
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact] public async void MultipleCloseAndDisposeIsValidOnDriver()
        {
            var driver = GraphDatabase.Driver("bolt://localhost");
            driver.Close();
            await driver.CloseAsync();
            driver.Dispose();

            var ex = Record.Exception(() => driver.Session());
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ObjectDisposedException>();
        }
    }
}
