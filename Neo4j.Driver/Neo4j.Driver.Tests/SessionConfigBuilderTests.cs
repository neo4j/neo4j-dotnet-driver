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
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests;

public class SessionConfigBuilderTests
{
    [Theory]
    [InlineData(Classification.Hint, Category.Hint)]
    [InlineData(Classification.Unrecognized, Category.Unrecognized)]
    [InlineData(Classification.Unsupported, Category.Unsupported)]
    [InlineData(Classification.Performance, Category.Performance)]
    [InlineData(Classification.Deprecation, Category.Deprecation)]
    [InlineData(Classification.Security, Category.Security)]
    [InlineData(Classification.Topology, Category.Topology)]
    [InlineData(Classification.Schema, Category.Schema)]
    [InlineData(Classification.Generic, Category.Generic)]
    public void WithNotifications_ShouldSetCategoryWithClassification(
        Classification classification,
        Category category)
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(null, disabledClassifications: [classification]);

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEquivalentTo([category]);

        config
            .Which
            .MinimumSeverity.Should()
            .Be(null);
    }

    [Theory]
    [InlineData(Category.Hint, Category.Hint)]
    [InlineData(Category.Unrecognized, Category.Unrecognized)]
    [InlineData(Category.Unsupported, Category.Unsupported)]
    [InlineData(Category.Performance, Category.Performance)]
    [InlineData(Category.Deprecation, Category.Deprecation)]
    [InlineData(Category.Security, Category.Security)]
    [InlineData(Category.Topology, Category.Topology)]
    [InlineData(Category.Schema, Category.Schema)]
    [InlineData(Category.Generic, Category.Generic)]
    public void WithNotifications_ShouldSetCategory(
        Category inCat,
        Category outCat)
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(null, [inCat]);

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEquivalentTo([outCat]);

        config
            .Which
            .MinimumSeverity.Should()
            .Be(null);
    }

    [Fact]
    public void WithNotifications_ShouldSetMultipleCategories()
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(null, [Category.Deprecation, Category.Hint]);

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEquivalentTo([Category.Deprecation, Category.Hint]);

        config
            .Which
            .MinimumSeverity.Should()
            .Be(null);
    }

    [Fact]
    public void WithNotifications_ShouldSetMultipleClassifications()
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(
            null,
            disabledClassifications: [Classification.Deprecation, Classification.Hint]);

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEquivalentTo([Category.Deprecation, Category.Hint]);

        config
            .Which
            .MinimumSeverity.Should()
            .Be(null);
    }

    [Fact]
    public void WithNotifications_ShouldSetSeverity()
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(Severity.Information, Array.Empty<Category>());

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEmpty();

        config
            .Which
            .MinimumSeverity.Should()
            .Be(Severity.Information);
    }

    [Fact]
    public void WithNotifications_ShouldSetSeverityWhenUsingClassification()
    {
        var configBuilder = new SessionConfigBuilder(new SessionConfig());

        configBuilder.WithNotifications(Severity.Warning, disabledClassifications: Array.Empty<Classification>());

        var config = configBuilder.Build()
            .NotificationsConfig.Should()
            .BeOfType<NotificationsConfig>();

        config
            .Which
            .DisabledCategories.Should()
            .BeEmpty();

        config
            .Which
            .MinimumSeverity.Should()
            .Be(Severity.Warning);
    }
}
