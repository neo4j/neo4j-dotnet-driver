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

using System;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal;

public class DriverCompositionTests
{
    [Fact]
    public void Has_no_disposal_surface_so_the_driver_owes_it_no_teardown()
    {
        var composition = typeof(DriverComposition);

        composition.Should().NotBeAssignableTo<IDisposable>();
        composition.Should().NotBeAssignableTo<IAsyncDisposable>();
    }
}
