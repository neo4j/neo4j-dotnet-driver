﻿// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.BookmarkManager;

public class BookmarkManagerFactoryTests
{
    [Fact]
    public void ShouldReturnNewBookmarkManager()
    {
        var factory = new BookmarkManagerFactory();

        var config = new BookmarkManagerConfig(
            Array.Empty<string>(),
            _ => Task.FromResult(Array.Empty<string>()),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = factory.NewBookmarkManager(config);

        bookmarkManager.Should().BeAssignableTo<DefaultBookmarkManager>();
    }

    [Fact]
    public void ShouldReturnDefaultBookmarkManagerWhenNoConfigSupplied()
    {
        var factory = new BookmarkManagerFactory();

        var bookmarkManager = factory.NewBookmarkManager();

        bookmarkManager.Should().BeAssignableTo<DefaultBookmarkManager>();
    }
}
