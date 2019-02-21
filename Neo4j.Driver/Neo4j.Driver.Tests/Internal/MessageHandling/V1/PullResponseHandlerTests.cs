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

namespace Neo4j.Driver.Internal.MessageHandling.V1
{
    public class PullResponseHandlerTests
    {
        [Fact]
        public void ShouldThrowIfStreamBuilderIsNull()
        {
            var summaryBuilder =
                new Mock<SummaryBuilder>(new Statement("stmt"), new ServerInfo(new Uri("bolt://localhost")));
            var exc = Record.Exception(() => new PullResponseHandler(null, summaryBuilder.Object));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("streamBuilder");
        }

        [Fact]
        public void ShouldThrowIfSummaryBuilderIsNull()
        {
            var exc = Record.Exception(() => new PullResponseHandler(new Mock<IResultStreamBuilder>().Object, null));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("summaryBuilder");
        }

        [Fact]
        public async Task ShouldCallPullCompletedOnSuccess()
        {
            var handler = CreateHandler(out var streamBuilder, out var summaryBuilder);

            await handler.OnSuccessAsync(new[]
            {
                CountersCollectorTests.TestMetadata, NotificationsCollectorTests.TestMetadata,
                PlanCollectorTests.TestMetadata, ProfiledPlanCollectorTests.TestMetadata,
                ResultConsumedAfterCollectorTests.TestMetadata, TypeCollectorTests.TestMetadata
            }.ToDictionary());

            streamBuilder.Verify(x => x.PullCompletedAsync(false, null), Times.Once);

            summaryBuilder.VerifySet(
                x => x.Counters = It.Is<ICounters>(c =>
                    c.ToString() == CountersCollectorTests.TestMetadataCollected.ToString()), Times.Once);
            summaryBuilder.VerifySet(x => x.Notifications = It.Is<IList<INotification>>(c =>
                c.ToString() == NotificationsCollectorTests.TestMetadataCollected.ToString()), Times.Once);
            summaryBuilder.VerifySet(x => x.Plan = It.Is<IPlan>(c =>
                c.ToString() == PlanCollectorTests.TestMetadataCollected.ToString()), Times.Once);
            summaryBuilder.VerifySet(x => x.Profile = It.Is<IProfiledPlan>(c =>
                c.ToString() == ProfiledPlanCollectorTests.TestMetadataCollected.ToString()), Times.Once);
            summaryBuilder.VerifySet(
                x => x.ResultConsumedAfter = ResultConsumedAfterCollectorTests.TestMetadataCollected, Times.Once);
            summaryBuilder.VerifySet(x => x.StatementType = TypeCollectorTests.TestMetadataCollected, Times.Once);
        }

        [Fact]
        public async Task ShouldCallPullCompletedOnFailure()
        {
            var error = new Mock<IResponsePipelineError>();
            var handler = CreateHandler(out var streamBuilder, out _);

            await handler.OnFailureAsync(error.Object);

            streamBuilder.Verify(x => x.PullCompletedAsync(false, error.Object), Times.Once);
        }

        [Fact]
        public async Task ShouldCallPullCompletedOnIgnored()
        {
            var handler = CreateHandler(out var streamBuilder, out _);

            await handler.OnIgnoredAsync();

            streamBuilder.Verify(x => x.PullCompletedAsync(false, null), Times.Once);
        }

        [Fact]
        public async Task ShouldCallPushRecordOnRecord()
        {
            var handler = CreateHandler(out var streamBuilder, out _);
            var fields = new object[] {1, "2", false};

            await handler.OnRecordAsync(fields);

            streamBuilder.Verify(x => x.PushRecordAsync(fields), Times.Once);
        }

        private static PullResponseHandler CreateHandler(out Mock<IResultStreamBuilder> streamBuilder,
            out Mock<SummaryBuilder> summaryBuilder)
        {
            summaryBuilder =
                new Mock<SummaryBuilder>(new Statement("stmt"), new ServerInfo(new Uri("bolt://localhost")));
            streamBuilder = new Mock<IResultStreamBuilder>();

            return new PullResponseHandler(streamBuilder.Object, summaryBuilder.Object);
        }
    }
}