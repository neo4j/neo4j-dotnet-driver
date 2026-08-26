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
using Moq;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class RemainingPropertiesMapperTests
{
    public interface ISampleBuilder
    {
        ISampleBuilder WithDatabase(string database);
        ISampleBuilder WithFetchSize(long fetchSize);
        ISampleBuilder WithTimeout(TimeSpan timeout);
    }

    private record SampleRequest
    {
        public string? Database { get; init; }
        public long? FetchSize { get; init; }
        public long? TimeoutMs { get; init; }
        public string? Handled { get; init; }
        public string? Unmappable { get; init; }
    }

    private readonly RemainingPropertiesMapper _mapper = new();
    private readonly Mock<ISampleBuilder> _builder = new();

    private static readonly IReadOnlySet<string> HandledExplicitly = new HashSet<string>
    {
        nameof(SampleRequest.Handled),
        nameof(SampleRequest.Unmappable)
    };

    [Fact]
    public void Applies_each_set_property_through_its_matching_builder_method()
    {
        _mapper.Apply(new SampleRequest { Database = "neo4j", FetchSize = 100 }, _builder.Object, HandledExplicitly);

        _builder.Verify(b => b.WithDatabase("neo4j"), Times.Once);
        _builder.Verify(b => b.WithFetchSize(100), Times.Once);
        _builder.VerifyNoOtherCalls();
    }

    [Fact]
    public void Skips_properties_that_are_null()
    {
        _mapper.Apply(new SampleRequest(), _builder.Object, HandledExplicitly);

        _builder.VerifyNoOtherCalls();
    }

    [Fact]
    public void Skips_properties_the_caller_handles_explicitly()
    {
        _mapper.Apply(new SampleRequest { Handled = "x" }, _builder.Object, HandledExplicitly);

        _builder.VerifyNoOtherCalls();
    }

    [Fact]
    public void Converts_an_Ms_suffixed_property_to_a_TimeSpan_on_the_unsuffixed_method()
    {
        _mapper.Apply(new SampleRequest { TimeoutMs = 5000 }, _builder.Object, HandledExplicitly);

        _builder.Verify(b => b.WithTimeout(TimeSpan.FromMilliseconds(5000)), Times.Once);
    }

    [Fact]
    public void Throws_naming_the_method_and_builder_when_no_builder_method_matches()
    {
        var handled = new HashSet<string> { nameof(SampleRequest.Handled) };

        var act = () => _mapper.Apply(new SampleRequest { Unmappable = "x" }, _builder.Object, handled);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithUnmappable*")
            .WithMessage($"*{nameof(ISampleBuilder)}*");
    }

    [Fact]
    public void Surfaces_the_builders_own_exception_unwrapped()
    {
        _builder.Setup(b => b.WithFetchSize(-5)).Throws(new ArgumentOutOfRangeException("size", "boom"));

        var act = () => _mapper.Apply(new SampleRequest { FetchSize = -5 }, _builder.Object, HandledExplicitly);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*boom*");
    }
}
