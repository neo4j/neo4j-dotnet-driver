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

// Base for the common "arrange mocks, run one method, assert" unit test. Set up
// dependencies with Freeze<T>()/Inject via Fixture, then build the subject once
// with CreateSubject<T>() (call it after arranging so frozen dependencies are used).
// Hand-roll tests directly against Fixture when the case isn't the basic shape.
//
// Non-generic on purpose: xUnit v3 only discovers public test classes, and an
// internal subject can't appear in a public class's base list, so the subject
// type is named at the CreateSubject<T>() call site instead.
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
