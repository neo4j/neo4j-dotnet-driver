// Copyright (c) "Neo4j"
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

#nullable enable

using AutoFixture;
using Moq;
using Neo4j.Driver.Tests.Internal.Core;

namespace Neo4j.Driver.Tests;

// Non-generic: an internal subject type can't appear in a public test class's base list (CS9338).
public abstract class UnitTestBase
{
    protected IFixture Fixture { get; }

    protected UnitTestBase(bool configureMembers = false)
    {
        Fixture = new Fixture().Customize(new DriverTestCustomization(configureMembers));
    }

    protected TSubject CreateSubject<TSubject>()
    {
        return Fixture.Create<TSubject>();
    }

    protected Mock<T> Freeze<T>() where T : class
    {
        return Fixture.Freeze<Mock<T>>();
    }
}
