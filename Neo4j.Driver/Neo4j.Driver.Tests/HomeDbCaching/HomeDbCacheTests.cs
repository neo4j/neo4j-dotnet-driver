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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.HomeDbCaching;
using Xunit;

namespace Neo4j.Driver.Tests.HomeDbCaching;

public class HomeDbCacheTests
{
    [Fact]
    public void ShouldAddAndRetrieveCacheItem()
    {
        // Arrange
        var cache = new HomeDbCache();
        var key = new HomeDbCacheKey("test-key");
        var databaseName = "testDatabase";

        // Act
        cache.AddOrUpdate(key, databaseName);
        var found = cache.TryGetCached(key, out var retrievedValue);

        // Assert
        found.Should().BeTrue();
        retrievedValue.Should().Be(databaseName);
    }

    [Fact]
    public void ShouldUpdateCacheItem()
    {
        // Arrange
        var cache = new HomeDbCache();
        var key = new HomeDbCacheKey("test-key");
        var initialDatabaseName = "initialDatabase";
        var updatedDatabaseName = "updatedDatabase";

        // Act
        cache.AddOrUpdate(key, initialDatabaseName);
        cache.AddOrUpdate(key, updatedDatabaseName);
        var found = cache.TryGetCached(key, out var retrievedValue);

        // Assert
        found.Should().BeTrue();
        retrievedValue.Should().Be(updatedDatabaseName);
    }

    [Fact]
    public void ShouldReturnFalseIfKeyNotFound()
    {
        // Arrange
        var cache = new HomeDbCache();
        var key = new HomeDbCacheKey("test-key");

        // Act
        var found = cache.TryGetCached(key, out var retrievedValue);

        // Assert
        found.Should().BeFalse();
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public void ShouldPurgeOldItemsWhenThresholdExceeded()
    {
        // Arrange
        var cache = new HomeDbCache();
        for (int i = 0; i < 10_001; i++)
        {
            var key = new HomeDbCacheKey($"test-key-{i}");
            cache.AddOrUpdate(key, $"database-{i}");
        }

        // Act
        var oldestKey = new HomeDbCacheKey("test-key-0");
        var found = cache.TryGetCached(oldestKey, out var retrievedValue);

        // Assert
        found.Should().BeFalse();
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public void ShouldMoveAccessedItemToFront()
    {
        // Arrange
        var cache = new HomeDbCache();
        var key1 = new HomeDbCacheKey("test-key-1");
        var key2 = new HomeDbCacheKey("test-key-2");
        cache.AddOrUpdate(key1, "database-1");
        cache.AddOrUpdate(key2, "database-2");

        // Act
        cache.TryGetCached(key1, out _); // Access key1
        cache.AddOrUpdate(new HomeDbCacheKey("test-key-3"), "database-3");

        // Assert
        cache.TryGetCached(key1, out var value1).Should().BeTrue();
        value1.Should().Be("database-1");
    }

    [Fact]
    public async Task ShouldBeThreadSafe()
    {
        // Arrange
        var cache = new HomeDbCache();
        var tasks = new List<Task>();
        var random = new Random();

        // Act
        for (int i = 0; i < 4; i++)
        {
            var task = Task.Run(
                () =>
                {
                    for (int j = 0; j < 250; j++)
                    {
                        var key = new HomeDbCacheKey($"key-{random.Next(0, 50)}");
                        var value = $"database-{random.Next(0, 50)}";

                        // Randomly perform one of the operations
                        switch (random.Next(0, 3))
                        {
                            case 0: // Add or update
                                cache.AddOrUpdate(key, value);
                                break;

                            case 1: // Try to retrieve
                                cache.TryGetCached(key, out _);
                                break;

                            case 2: // Remove and re-add
                                cache.AddOrUpdate(key, value);
                                cache.TryGetCached(key, out _);
                                break;
                        }
                    }
                });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        // If no exceptions are thrown, the test passes
    }
}
