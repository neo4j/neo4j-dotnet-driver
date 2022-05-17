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
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Reactive
{
    public class DriverExtensionsTests
    {
        [Fact]
        public void ShouldThrowIfDriverIsNotOfExpectedType()
        {
            Action act = () => NewSession(Mock.Of<IDriver>());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldReturnRxSession()
        {
            var driver = GraphDatabase.Driver("bolt://localhost");

            NewSession(driver).Should().BeOfType<InternalRxSession>();
        }

        private static IRxSession NewSession(IDriver driver)
        {
            return driver.RxSession(o =>
                o.WithDefaultAccessMode(AccessMode.Write).WithBookmarks(Bookmarks.From("1", "3")));
        }
    }
}