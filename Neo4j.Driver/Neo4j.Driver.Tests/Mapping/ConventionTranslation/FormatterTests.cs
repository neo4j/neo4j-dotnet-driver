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
using FluentAssertions;
using Neo4j.Driver.Mapping.ConventionTranslation;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping.ConventionTranslation;

public class FormatterTests
{
    [Fact]
    public void SnakeCaseCombiner_ShouldFormat()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.SnakeCase);
        var result = combiner.Format(new[] { "aPple", "BANANA", "Cherry" });
        result.Should().Be("apple_banana_cherry");
    }

    [Fact]
    public void SnakeCaseCombiner_ShouldCombineSingleToken()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.SnakeCase);
        var result = combiner.Format(new[] { "aPple" });
        result.Should().Be("apple");
    }

    [Fact]
    public void CamelCaseCombiner_ShouldFormat()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var result = combiner.Format(new[] { "aPple", "BANANA", "Cherry" });
        result.Should().Be("appleBananaCherry");
    }

    [Fact]
    public void CamelCaseCombiner_ShouldCombineSingleToken()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.CamelCase);
        var result = combiner.Format(new[] { "aPple" });
        result.Should().Be("apple");
    }

    [Fact]
    public void PascalCaseCombiner_ShouldFormat()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.PascalCase);
        var result = combiner.Format(new[] { "aPple", "BANANA", "Cherry" });
        result.Should().Be("AppleBananaCherry");
    }

    [Fact]
    public void PascalCaseCombiner_ShouldCombineSingleToken()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.PascalCase);
        var result = combiner.Format(new[] { "aPple" });
        result.Should().Be("Apple");
    }

    [Fact]
    public void ScreamingSnakeCaseCombiner_ShouldFormat()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.ScreamingSnakeCase);
        var result = combiner.Format(new[] { "aPple", "BANANA", "Cherry" });
        result.Should().Be("APPLE_BANANA_CHERRY");
    }

    [Fact]
    public void ScreamingSnakeCaseCombiner_ShouldCombineSingleToken()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.ScreamingSnakeCase);
        var result = combiner.Format(new[] { "aPple" });
        result.Should().Be("APPLE");
    }

    [Fact]
    public void KebabCaseCombiner_ShouldFormat()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.KebabCase);
        var result = combiner.Format(new[] { "aPple", "BANANA", "Cherry" });
        result.Should().Be("apple-banana-cherry");
    }

    [Fact]
    public void KebabCaseCombiner_ShouldCombineSingleToken()
    {
        var combiner = new StandardCaseFormatter(FieldCaseConvention.KebabCase);
        var result = combiner.Format(new[] { "aPple" });
        result.Should().Be("apple");
    }
}
