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

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewSessionConfigMapperTests
{
    private readonly AutoMocker _autoMocker;
    private readonly Mock<ISessionConfigBuilder> _builder;

    public NewSessionConfigMapperTests()
    {
        _autoMocker = AutoMocker.ForTesting<NewSessionConfigMapper>();
        _builder = _autoMocker.GetMock<ISessionConfigBuilder>();
    }

    private static NewSessionRequest MinimalRequest()
    {
        return new NewSessionRequest
        {
            Driver = new RegistryObject<IDriver>("driver-1", Mock.Of<IDriver>()),
            AccessMode = "r"
        };
    }

    private void Apply(NewSessionRequest request)
    {
        var mapper = _autoMocker.CreateInstance<NewSessionConfigMapper>();
        mapper.Apply(request, _builder.Object);
    }

    [Fact]
    public void Calls_only_the_access_mode_when_no_other_field_is_set()
    {
        Apply(MinimalRequest());

        _builder.Verify(b => b.WithDefaultAccessMode(AccessMode.Read), Times.Once);
        _builder.Invocations.Should().HaveCount(1);
    }

    [Fact]
    public void Maps_r_to_read_access_mode()
    {
        Apply(MinimalRequest() with { AccessMode = "r" });

        _builder.Verify(b => b.WithDefaultAccessMode(AccessMode.Read), Times.Once);
    }

    [Fact]
    public void Maps_w_to_write_access_mode()
    {
        Apply(MinimalRequest() with { AccessMode = "w" });

        _builder.Verify(b => b.WithDefaultAccessMode(AccessMode.Write), Times.Once);
    }

    [Fact]
    public void Resolves_and_applies_the_bookmark_manager_by_id()
    {
        var manager = Mock.Of<IBookmarkManager>();
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Get<IBookmarkManager>("bm-1"))
            .Returns(new RegistryObject<IBookmarkManager>("bm-1", manager));

        Apply(MinimalRequest() with { BookmarkManagerId = "bm-1" });

        _builder.Verify(b => b.WithBookmarkManager(manager), Times.Once);
    }

    [Fact]
    public void Maps_bookmarks_to_a_bookmarks_instance()
    {
        Apply(MinimalRequest() with { Bookmarks = ["bm:1", "bm:2"] });

        _builder.Verify(
            b => b.WithBookmarks(It.Is<Bookmarks>(bm => bm.Values.SequenceEqual(new[] { "bm:1", "bm:2" }))),
            Times.Once);
    }

    [Fact]
    public void Leaves_bookmarks_unset_when_absent()
    {
        Apply(MinimalRequest());

        _builder.Verify(b => b.WithBookmarks(It.IsAny<Bookmarks[]>()), Times.Never);
    }

    [Fact]
    public void Maps_database_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { Database = "neo4j" });

        _builder.Verify(b => b.WithDatabase("neo4j"), Times.Once);
    }

    [Fact]
    public void Maps_fetchSize_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { FetchSize = 100 });

        _builder.Verify(b => b.WithFetchSize(100), Times.Once);
    }

    [Fact]
    public void Maps_impersonatedUser_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { ImpersonatedUser = "alice" });

        _builder.Verify(b => b.WithImpersonatedUser("alice"), Times.Once);
    }

    [Fact]
    public void Maps_disableAutoCommitRetries_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { DisableAutoCommitRetries = true });

        _builder.Verify(b => b.WithDisableAutoCommitRetries(true), Times.Once);
    }

    [Fact]
    public void Maps_the_authorization_token_via_the_special_case()
    {
        Apply(MinimalRequest() with { AuthorizationToken = new AuthorizationToken("basic", "neo4j", "secret") });

        _builder.Verify(b => b.WithAuthToken(It.IsAny<IAuthToken>()), Times.Once);
    }

    [Fact]
    public void Leaves_the_authorization_token_unset_when_absent()
    {
        Apply(MinimalRequest());

        _builder.Verify(b => b.WithAuthToken(It.IsAny<IAuthToken>()), Times.Never);
    }

    [Fact]
    public void Disables_notifications_when_minimum_severity_is_OFF()
    {
        Apply(MinimalRequest() with { NotificationsMinSeverity = "OFF" });

        _builder.Verify(b => b.WithNotificationsDisabled(), Times.Once);
    }

    [Fact]
    public void Maps_notification_severity_and_categories_to_one_WithNotifications_call()
    {
        Apply(
            MinimalRequest() with
            {
                NotificationsMinSeverity = "WARNING",
                NotificationsDisabledCategories = ["HINT", "GENERIC"]
            });

        _builder.Verify(
            b => b.WithNotifications(
                Severity.Warning,
                It.Is<Category[]>(c => c.SequenceEqual(new[] { Category.Hint, Category.Generic }))),
            Times.Once);
    }

    [Fact]
    public void Maps_notification_categories_alone_when_severity_is_absent()
    {
        Apply(MinimalRequest() with { NotificationsDisabledCategories = ["SECURITY"] });

        _builder.Verify(
            b => b.WithNotifications(null, It.Is<Category[]>(c => c.SequenceEqual(new[] { Category.Security }))),
            Times.Once);
    }

    [Fact]
    public void Leaves_notifications_unset_when_both_fields_are_absent()
    {
        Apply(MinimalRequest());

        _builder.Verify(b => b.WithNotificationsDisabled(), Times.Never);
        _builder.Verify(
            b => b.WithNotifications(It.IsAny<Severity?>(), It.IsAny<Category[]>()),
            Times.Never);
    }

    [Fact]
    public void Throws_when_a_fallback_tier_property_has_no_matching_builder_method()
    {
        var request = new RequestWithUnmappedProperty(MinimalRequest()) { Nonexistent = "value" };

        var act = () => Apply(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Nonexistent*");
    }

    [Fact]
    public void Surfaces_the_builders_own_exception_instead_of_a_TargetInvocationException()
    {
        _builder.Setup(b => b.WithFetchSize(-5)).Throws(new ArgumentOutOfRangeException("size", "boom"));

        var act = () => Apply(MinimalRequest() with { FetchSize = -5 });

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*boom*");
    }

    private record RequestWithUnmappedProperty : NewSessionRequest
    {
        [SetsRequiredMembers]
        public RequestWithUnmappedProperty(NewSessionRequest source) : base(source)
        {
        }

        public string? Nonexistent { get; init; }
    }
}
