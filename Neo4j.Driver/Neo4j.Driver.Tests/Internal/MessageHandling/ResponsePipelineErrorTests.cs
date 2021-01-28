// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling
{
    public class ResponsePipelineErrorTests
    {
        [Fact]
        public void ShouldThrowIfExceptionIsNull()
        {
            var exc = Record.Exception(() => new ResponsePipelineError(null));

            exc.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("exception");
        }

        [Fact]
        public void ShouldReturnTrueIfIsT()
        {
            var err = new ResponsePipelineError(new ArgumentOutOfRangeException());

            err.Is<ArgumentOutOfRangeException>().Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnFalseIfIsNotT()
        {
            var err = new ResponsePipelineError(new ArgumentOutOfRangeException());

            err.Is<ProtocolException>().Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldReturnPredicateResultOnIs(bool predicateReturns)
        {
            var err = new ResponsePipelineError(new ArgumentOutOfRangeException());

            err.Is(e => predicateReturns).Should().Be(predicateReturns);
        }

        [Fact]
        public void ShouldThrowIfNotThrown()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrown());

            recordedExc.Should().Be(exc);
        }

        [Fact]
        public void ShouldNotThrowIfThrown()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrown());
            recordedExc.Should().Be(exc);

            var recordedExc2 = Record.Exception(() => err.EnsureThrown());
            recordedExc2.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfIsT()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrownIf<ArgumentOutOfRangeException>());

            recordedExc.Should().Be(exc);
        }

        [Fact]
        public void ShouldNotThrowIfIsNotT()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrownIf<ProtocolException>());

            recordedExc.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfPredicateIsTrue()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrownIf(e => true));

            recordedExc.Should().Be(exc);
        }

        [Fact]
        public void ShouldNotThrowIfPredicateIsFalse()
        {
            var exc = new ArgumentOutOfRangeException();
            var err = new ResponsePipelineError(exc);

            var recordedExc = Record.Exception(() => err.EnsureThrownIf(e => false));

            recordedExc.Should().BeNull();
        }
    }
}