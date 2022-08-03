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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.V3
{
    public class CommitResponseHandlerTests
    {
        [Fact]
        public void ShouldThrowIfBookmarkTrackerIsNull()
        {
            var exc = Record.Exception(() => new CommitResponseHandler(null));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("tracker");
        }

        [Fact]
        public void ShouldUpdateBookmark()
        {
            var tracker = new Mock<IBookmarksTracker>();
            var handler = new CommitResponseHandler(tracker.Object);

            handler.OnSuccess(new[] {BookmarkCollectorTests.TestMetadata}.ToDictionary());

            tracker.Verify(
                x => x.UpdateBookmarks(BookmarkCollectorTests.TestMetadataCollected, null),
                Times.Once);
        }
    }
}