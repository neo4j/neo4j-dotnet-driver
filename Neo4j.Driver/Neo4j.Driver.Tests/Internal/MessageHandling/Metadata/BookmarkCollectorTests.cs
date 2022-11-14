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
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class BookmarkCollectorTests
    {
        private const string Key = BookmarksCollector.BookmarkKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new BookmarksCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new BookmarksCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new BookmarksCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should().Contain($"Expected '{Key}' metadata to be of type 'String', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var bookmarkStr = "bookmark-455";
            var metadata = new Dictionary<string, object> {{Key, bookmarkStr}};
            var collector = new BookmarksCollector();

            collector.Collect(metadata);

            collector.Collected.Should().BeEquivalentTo(Bookmarks.From(bookmarkStr));
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var bookmarkStr = "bookmark-455";
            var metadata = new Dictionary<string, object> {{Key, bookmarkStr}};
            var collector = new BookmarksCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().BeSameAs(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, "bookmark-455");

        internal static Bookmarks TestMetadataCollected => Bookmarks.From((string) TestMetadata.Value);
    }
}