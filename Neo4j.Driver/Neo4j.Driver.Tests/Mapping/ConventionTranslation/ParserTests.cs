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
using Neo4j.Driver.Mapping.ConventionTranslation;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.ConventionTranslation;

public class ParserTests
{
    [Fact]
    public void ShouldParseSnakeCaseTokens()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.SnakeCase);
        var tokens = parser.ParseIdentifier("apple_banana_cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldParseCamelCaseTokens()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CamelCase);
        var tokens = parser.ParseIdentifier("appleBananaCherry");
        tokens.Should().BeEquivalentTo("apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldParsePascalCaseTokens()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.PascalCase);
        var tokens = parser.ParseIdentifier("AppleBananaCherry");
        tokens.Should().BeEquivalentTo("Apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldParseScreamingSnakeCaseTokens()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.ScreamingSnakeCase);
        var tokens = parser.ParseIdentifier("APPLE_BANANA_CHERRY");
        tokens.Should().BeEquivalentTo("APPLE", "BANANA", "CHERRY");
    }

    [Fact]
    public void ShouldParseKebabCaseTokens()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.KebabCase);
        var tokens = parser.ParseIdentifier("apple-banana-cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldParseCSharpIdentifierTokens_LowerCaseStart()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier);
        var tokens = parser.ParseIdentifier("appleBananaCherry");
        tokens.Should().BeEquivalentTo("apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldParseCSharpIdentifierTokens_UpperCaseStart()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier);
        var tokens = parser.ParseIdentifier("AppleBananaCherry");
        tokens.Should().BeEquivalentTo("Apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidCamelCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CamelCase);
        Action act = () => parser.ParseIdentifier("AppleBananaCherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidPascalCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.PascalCase);
        Action act = () => parser.ParseIdentifier("appleBananaCherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidSnakeCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.SnakeCase);
        Action act = () => parser.ParseIdentifier("apple-Banana-Cherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidScreamingSnakeCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.ScreamingSnakeCase);
        Action act = () => parser.ParseIdentifier("APPLE-banana-CHERRY");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidKebabCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.KebabCase);
        Action act = () => parser.ParseIdentifier("apple_Banana_Cherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidCSharpIdentifier()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CSharpIdentifier);
        Action act = () => parser.ParseIdentifier("apple_bananaCherry");
        act.Should().Throw<ArgumentException>();
    }
}
