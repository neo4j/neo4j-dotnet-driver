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

public class ExtractorTests
{
    [Fact]
    public void ShouldExtractSnakeCaseTokens()
    {
        var extractor = new SnakeCaseExtractor();
        var tokens = extractor.ExtractTokens("apple_banana_cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractCamelCaseTokens()
    {
        var extractor = new CamelCaseExtractor();
        var tokens = extractor.ExtractTokens("appleBananaCherry");
        tokens.Should().BeEquivalentTo("apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldExtractPascalCaseTokens()
    {
        var extractor = new PascalCaseExtractor();
        var tokens = extractor.ExtractTokens("AppleBananaCherry");
        tokens.Should().BeEquivalentTo("Apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldExtractScreamingSnakeCaseTokens()
    {
        var extractor = new ScreamingSnakeCaseExtractor();
        var tokens = extractor.ExtractTokens("APPLE_BANANA_CHERRY");
        tokens.Should().BeEquivalentTo("APPLE", "BANANA", "CHERRY");
    }

    [Fact]
    public void ShouldExtractKebabCaseTokens()
    {
        var extractor = new KebabCaseExtractor();
        var tokens = extractor.ExtractTokens("apple-banana-cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractCSharpIdentifierTokens_LowerCaseStart()
    {
        var extractor = new CSharpIdentifierExtractor();
        var tokens = extractor.ExtractTokens("appleBananaCherry");
        tokens.Should().BeEquivalentTo("apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldExtractCSharpIdentifierTokens_UpperCaseStart()
    {
        var extractor = new CSharpIdentifierExtractor();
        var tokens = extractor.ExtractTokens("AppleBananaCherry");
        tokens.Should().BeEquivalentTo("Apple", "Banana", "Cherry");
    }

    [Fact]
    public void ShouldExtractTokensWithCustomRegex_Dot()
    {
        var extractor = new RegexExtractor(@"\.", @"\.");
        var tokens = extractor.ExtractTokens("apple.banana.cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractTokensWithCustomRegex_Space()
    {
        var extractor = new RegexExtractor(@"\s+", @"\s+");
        var tokens = extractor.ExtractTokens("apple banana cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractTokensWithCustomRegex_Semicolon()
    {
        var extractor = new RegexExtractor(";", ";");
        var tokens = extractor.ExtractTokens("apple;banana;cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractTokensWithCustomRegex_Pipe()
    {
        var extractor = new RegexExtractor(@"\|", @"\|");
        var tokens = extractor.ExtractTokens("apple|banana|cherry");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry");
    }

    [Fact]
    public void ShouldExtractTokensWithCustomRegex_MixedDelimiters()
    {
        var extractor = new RegexExtractor(@"[_\-\.\s]+", @"[_\-\.\s]+");
        var tokens = extractor.ExtractTokens("apple_banana-cherry.mango pineapple");
        tokens.Should().BeEquivalentTo("apple", "banana", "cherry", "mango", "pineapple");
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidMixedDelimiters()
    {
        var extractor = new RegexExtractor(@"^([a-zA-Z]+[_\-\.\s])*[a-zA-Z]+$", @"[_\-\.\s]+");
        Action act = () => extractor.ExtractTokens("apple_banana-cherry.mango pineapple!");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidCamelCase()
    {
        var extractor = new CamelCaseExtractor();
        Action act = () => extractor.ExtractTokens("AppleBananaCherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidPascalCase()
    {
        var extractor = new PascalCaseExtractor();
        Action act = () => extractor.ExtractTokens("appleBananaCherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidSnakeCase()
    {
        var extractor = new SnakeCaseExtractor();
        Action act = () => extractor.ExtractTokens("apple-Banana-Cherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidScreamingSnakeCase()
    {
        var extractor = new ScreamingSnakeCaseExtractor();
        Action act = () => extractor.ExtractTokens("APPLE-banana-CHERRY");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidKebabCase()
    {
        var extractor = new KebabCaseExtractor();
        Action act = () => extractor.ExtractTokens("apple_Banana_Cherry");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowExceptionForInvalidCSharpIdentifier()
    {
        var extractor = new CSharpIdentifierExtractor();
        Action act = () => extractor.ExtractTokens("apple_bananaCherry");
        act.Should().Throw<ArgumentException>();
    }
}
