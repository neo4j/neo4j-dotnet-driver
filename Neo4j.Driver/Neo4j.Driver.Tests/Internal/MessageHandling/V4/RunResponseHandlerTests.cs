// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.MessageHandling.V4
{
    public class RunResponseHandlerTests
    {
        [Fact]
        public void ShouldThrowIfStreamBuilderIsNull()
        {
            var summaryBuilder =
                new Mock<SummaryBuilder>(new Statement("stmt"), new ServerInfo(new Uri("bolt://localhost")));
            var exc = Record.Exception(() => new RunResponseHandler(null, summaryBuilder.Object));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("streamBuilder");
        }

        [Fact]
        public void ShouldThrowIfSummaryBuilderIsNull()
        {
            var exc = Record.Exception(() => new RunResponseHandler(new Mock<IResultStreamBuilder>().Object, null));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("summaryBuilder");
        }

        [Fact]
        public void ShouldCallRunCompletedOnSuccess()
        {
            var handler = CreateHandler(out var streamBuilder, out var summaryBuilder);

            handler.OnSuccess(new[]
            {
                FieldsCollectorTests.TestMetadata, TimeToFirstCollectorTests.TestMetadata,
                QueryIdCollectorTests.TestMetadata
            }.ToDictionary());

            streamBuilder.Verify(
                x => x.RunCompleted(QueryIdCollectorTests.TestMetadataCollected,
                    FieldsCollectorTests.TestMetadataCollected, null),
                Times.Once);

            summaryBuilder.VerifySet(
                x => x.ResultAvailableAfter = TimeToFirstCollectorTests.TestMetadataCollected, Times.Once);
        }

        [Fact]
        public void ShouldCallRunCompletedOnFailure()
        {
            var error = new Mock<IResponsePipelineError>();
            var handler = CreateHandler(out var streamBuilder, out _);

            handler.OnFailure(error.Object);

            streamBuilder.Verify(x => x.RunCompleted(-1, null, error.Object), Times.Once);
        }

        [Fact]
        public void ShouldCallRunCompletedOnIgnored()
        {
            var handler = CreateHandler(out var streamBuilder, out _);

            handler.OnIgnored();

            streamBuilder.Verify(x => x.RunCompleted(-1, null, null), Times.Once);
        }

        private static RunResponseHandler CreateHandler(out Mock<IResultStreamBuilder> streamBuilder,
            out Mock<SummaryBuilder> summaryBuilder)
        {
            summaryBuilder =
                new Mock<SummaryBuilder>(new Statement("stmt"), new ServerInfo(new Uri("bolt://localhost")));
            streamBuilder = new Mock<IResultStreamBuilder>();

            return new RunResponseHandler(streamBuilder.Object, summaryBuilder.Object);
        }
    }
}