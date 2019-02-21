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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling
{
    public class NoOpResponseHandlerTests
    {
        [Fact]
        public async Task ShouldCompleteOnSuccess()
        {
            await new NoOpResponseHandler().OnSuccessAsync(new Dictionary<string, object>());
        }

        [Fact]
        public async Task ShouldCompleteOnFailure()
        {
            await new NoOpResponseHandler().OnFailureAsync(new Mock<IResponsePipelineError>().Object);
        }

        [Fact]
        public async Task ShouldCompleteOnIgnored()
        {
            await new NoOpResponseHandler().OnIgnoredAsync();
        }

        [Fact]
        public async Task ShouldThrowOnRecord()
        {
            var ex = await Record.ExceptionAsync(() => new NoOpResponseHandler().OnRecordAsync(new object[0]));

            ex.Should().BeOfType<ProtocolException>().Which.Message.Should()
                .Be("OnRecordAsync is not expected at this time.");
        }
    }
}