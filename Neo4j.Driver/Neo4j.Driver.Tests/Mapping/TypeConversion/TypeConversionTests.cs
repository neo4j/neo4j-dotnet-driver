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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.TypeConversion;

public class TypeConversionTests : MappingTestWithGlobalState
{
    private class WebSite
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
    }

    private class User
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
    }

    private class UserAndWebSite
    {
        public User User { get; set; }
        public WebSite WebSite { get; set; }
    }

    [Fact]
    public void ShouldFailToMapRecordToClassWithoutConversion()
    {
        var record = TestRecord.Create(("Name", "Neo4j"), ("Uri", "http://neo4j.com"));

        var act = () => { _ = record.AsObject<WebSite>(); };

        act.Should().Throw<MappingFailedException>();
    }

    [Fact]
    public void ShouldMapRecordToClassWithConversion()
    {
        var record = TestRecord.Create(("Name", "Neo4j"), ("Uri", "http://neo4j.com"));
        RecordObjectMapping.RegisterTypeConverter((string s) => new Uri(s));

        var website = record.AsObject<WebSite>();

        website.Name.Should().Be("Neo4j");
        website.Uri.Should().Be(new Uri("http://neo4j.com"));
    }

    [Fact]
    public void ShouldMapWhenUsingBlueprintMapping()
    {
        const string guidStr = "123e4567-e89b-12d3-a456-426614174000";
        var record = TestRecord.Create(("Name", "Neo4j"), ("Id", guidStr));
        RecordObjectMapping.RegisterTypeConverter((string s) => Guid.Parse(s));
        var user = record.AsObjectFromBlueprint(new { Name = default(string), Id = default(Guid) });
        user.Name.Should().Be("Neo4j");
        user.Id.Should().Be(Guid.Parse(guidStr));
    }

    [Fact]
    public void ShouldMapWhenUsingDelegateMapping()
    {
        const string guidStr = "123e4567-e89b-12d3-a456-426614174999";
        var record = TestRecord.Create(("name", "Neo4j"), ("id", guidStr));
        RecordObjectMapping.RegisterTypeConverter((string s) => Guid.Parse(s));

        var user = record.AsObject((string name, Guid id) => new User { Name = name, Id = id });

        user.Name.Should().Be("Neo4j");
        user.Id.Should().Be(Guid.Parse(guidStr));
    }

    [Fact]
    public void ShouldUseConverterDuringNestedMapping()
    {
        var websiteEntity = new Node(1, new[] {"WebSite"}, new Dictionary<string, object>
        {
            {"Name", "Neo4j"},
            {"Uri", "http://neo4j.com"}
        });

        var userId = Guid.NewGuid();
        var userEntity = new Node(2, new[] {"User"}, new Dictionary<string, object>
        {
            {"Name", "John"},
            {"Id", userId.ToString()}
        });

        var testRecord = TestRecord.Create(("WebSite", websiteEntity), ("User", userEntity));
        RecordObjectMapping.RegisterTypeConverter((string s) => Guid.Parse(s));
        RecordObjectMapping.RegisterTypeConverter((string s) => new Uri(s));

        var userAndWebSite = testRecord.AsObject<UserAndWebSite>();

        userAndWebSite.User.Name.Should().Be("John");
        userAndWebSite.User.Id.Should().Be(userId);
        userAndWebSite.WebSite.Name.Should().Be("Neo4j");
        userAndWebSite.WebSite.Uri.Should().Be(new Uri("http://neo4j.com"));
    }

    [Fact]
    public void ShouldUseConverterWhenMappingLists()
    {
        var uriStringList = new List<string> {"http://neo4j.com", "http://google.com", "http://bing.com", "http://yahoo.com"};
        var expected = uriStringList.Select(s => new Uri(s));
        var record = TestRecord.Create(("UriList", uriStringList));
        RecordObjectMapping.RegisterTypeConverter((string s) => new Uri(s));

        var mapped = record.AsObjectFromBlueprint(new { UriList = default(List<Uri>) });

        mapped.UriList.Should().BeEquivalentTo(expected);
    }
}
