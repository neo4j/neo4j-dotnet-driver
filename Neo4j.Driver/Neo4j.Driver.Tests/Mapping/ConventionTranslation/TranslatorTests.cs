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

using System.Collections.Generic;
using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Mapping.ConventionTranslation;
using Neo4j.Driver.Mapping.ConventionTranslation;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.ConventionTranslation;

public class TranslatorTests
{
    private readonly AutoMocker _mocker = new();

    [Fact]
    public void ShouldTranslate()
    {
        var fruits = new List<string> { "Apple", "Banana", "Cherry" };

        _mocker.GetMock<IIdentifierParser<IReadOnlyList<string>>>()
                .Setup(x => x.ParseIdentifier("AppleBananaCherry"))
                .Returns(fruits);

        _mocker.GetMock<IFieldFormatter<IReadOnlyList<string>>>()
            .Setup(x => x.Format(fruits))
            .Returns("apple_banana_cherry");

        var translator = _mocker.CreateInstance<ConventionTranslator<IReadOnlyList<string>>>();
        var result = translator.Translate("AppleBananaCherry");
        result.Should().Be("apple_banana_cherry");
    }

    [Fact]
    public void ShouldTranslateFromCamelCaseToSnakeCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CamelCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.SnakeCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("appleBananaCherry");
        result.Should().Be("apple_banana_cherry");
    }

    [Fact]
    public void ShouldTranslateFromPascalCaseToKebabCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.PascalCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.KebabCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("AppleBananaCherry");
        result.Should().Be("apple-banana-cherry");
    }

    [Fact]
    public void ShouldTranslateFromSnakeCaseToCamelCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.SnakeCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("apple_banana_cherry");
        result.Should().Be("appleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateFromKebabCaseToPascalCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.KebabCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.PascalCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("apple-banana-cherry");
        result.Should().Be("AppleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateFromScreamingSnakeCaseToCamelCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.ScreamingSnakeCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("APPLE_BANANA_CHERRY");
        result.Should().Be("appleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateCSharpIdentifierFromCamelCaseToCamelCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CamelCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("appleBananaCherry");
        result.Should().Be("appleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateCSharpIdentifierFromPascalCaseToPascalCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.PascalCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.PascalCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("AppleBananaCherry");
        result.Should().Be("AppleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateCSharpIdentifierFromCamelCaseToPascalCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.CamelCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.PascalCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("appleBananaCherry");
        result.Should().Be("AppleBananaCherry");
    }

    [Fact]
    public void ShouldTranslateCSharpIdentifierFromPascalCaseToCamelCase()
    {
        var parser = new StandardCaseParser(IdentifierCaseConvention.PascalCase);
        var formatter = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var translator = new ConventionTranslator<IReadOnlyList<string>>(parser, formatter);
        var result = translator.Translate("AppleBananaCherry");
        result.Should().Be("appleBananaCherry");
    }

    [Fact]
    public void NoOpTranslator_ShouldNotTranslate()
    {
        var translator = new NoOpConventionTranslator();
        var result = translator.Translate("appleBananaCherry");
        result.Should().Be("appleBananaCherry");
    }
}
