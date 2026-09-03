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

using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

// The current FakeTime implementation in the driver is a hack, using a static instance.
// The instance is modified by tests only; if we put the tests that use FakeTime in this
// collection then they won't trample each other's toes.
[CollectionDefinition(Name)]
public class FakeSystemClockCollection
{
    public const string Name = "Fake system clock";
}
