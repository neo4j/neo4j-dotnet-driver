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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SummaryCollectorV3Tests
    {
        private static SummaryCollectorV3 NewSummaryCollector()
        {
            return new SummaryCollectorV3(new Statement(null), new Mock<IServerInfo>().Object);
        }
        
        public class CollectBookmarkMethod
        {
            [Fact]
            public void ShouldCollectBookmark()
            {
                var mock = new Mock<IDictionary<string, object>>();
                var collector = NewSummaryCollector();
                collector.CollectBookmark(mock.Object);
                
                mock.Verify(x=>x.ContainsKey("bookmark"), Times.Once);
            }
        }
        
        public class CollectMethod
        {
            [Fact]
            public void ShouldCollectAllItems()
            {
                var mock = new Mock<IDictionary<string, object>>();
                var collector = NewSummaryCollector();
                collector.Collect(mock.Object);

                mock.Verify(x=>x.ContainsKey("type"), Times.Once);
                mock.Verify(x=>x.ContainsKey("stats"), Times.Once);
                mock.Verify(x=>x.ContainsKey("plan"), Times.Once);
                mock.Verify(x=>x.ContainsKey("profile"), Times.Once);
                mock.Verify(x=>x.ContainsKey("notifications"), Times.Once);
                mock.Verify(x=>x.ContainsKey("t_last"), Times.Once);
            }
        }
        
        public class CollectWithFields
        {
            [Fact]
            public void ShouldCollectAllItems()
            {
                var mock = new Mock<IDictionary<string, object>>();
                var collector = NewSummaryCollector();
                collector.CollectWithFields(mock.Object);

                mock.Verify(x => x.ContainsKey("t_first"), Times.Once);
            }
        }
        
        public class ResultAvailableAndConsumedAfterMethod
        {
            [Fact]
            public void ShouldCollectResultAvailableAfter()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"fields",  new List<object>() },
                    {"t_first", 12345},
                    {"t_last", 67890}
                };

                var collector = NewSummaryCollector();;
                collector.CollectWithFields(meta);
                var summary = collector.Build();

                summary.ResultAvailableAfter.ToString().Should().Be("00:00:12.3450000");
                summary.ResultConsumedAfter.ToString().Should().Be("-00:00:00.0010000");
            }

            [Fact]
            public void ShouldCollectResultConsumedAfter()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"t_first", 12345},
                    {"t_last", 67890}
                };

                var collector = NewSummaryCollector();;
                collector.Collect(meta);
                var summary = collector.Build();

                summary.ResultAvailableAfter.ToString().Should().Be("-00:00:00.0010000");
                summary.ResultConsumedAfter.ToString().Should().Be("00:01:07.8900000");
            }
        }
    }
}