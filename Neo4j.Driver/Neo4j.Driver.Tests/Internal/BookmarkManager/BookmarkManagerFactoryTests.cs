// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Neo4j.Driver.Internal.BookmarkManager;

public class BookmarkManagerFactoryTests
{
    [Fact]
    public void ShouldReturnNewBookmarkManager()
    {
        var factory = new BookmarkManagerFactory();

        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = factory.NewBookmarkManager(config);

        bookmarkManager.Should().BeAssignableTo<DefaultBookmarkManager>();
    }
    [Fact]
    public void ShouldReturnNoOpBookmarkManagerOnNullConfig()
    {
        var factory = new BookmarkManagerFactory();

        var bookmarkManager = factory.NewBookmarkManager(null);

        bookmarkManager.Should().BeAssignableTo<NoOpBookmarkManager>();
    }
}