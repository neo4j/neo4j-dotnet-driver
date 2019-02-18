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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class NotificationsCollectorTests
    {
        public const string Key = NotificationsCollector.NotificationsKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new NotificationsCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new NotificationsCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfValueIsNull()
        {
            var collector = new NotificationsCollector();

            collector.Collect(new Dictionary<string, object> {{Key, null}});

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, 3}};
            var collector = new NotificationsCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'List<Object>', but got 'Int32'.");
        }

        [Fact]
        public void ShouldCollect()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            {"code", "code1"},
                            {"title", "title1"},
                            {"description", "description1"},
                            {"severity", "severity1"},
                            {
                                "position", new Dictionary<string, object>
                                {
                                    {"offset", 1L},
                                    {"line", 2L},
                                    {"column", 3L}
                                }
                            }
                        }
                    }
                }
            };
            var collector = new NotificationsCollector();

            collector.Collect(metadata);

            collector.Collected.ShouldBeEquivalentTo(new[]
            {
                new Notification("code1", "title1", "description1",
                    new InputPosition(1, 2, 3), "severity1")
            });
        }

        [Fact]
        public void ShouldCollectList()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            {"code", "code1"},
                            {"title", "title1"},
                            {"description", "description1"},
                            {"severity", "severity1"},
                            {
                                "position", new Dictionary<string, object>
                                {
                                    {"offset", 1L},
                                    {"line", 2L},
                                    {"column", 3L}
                                }
                            }
                        },
                        new Dictionary<string, object>
                        {
                            {"code", "code2"},
                            {"title", "title2"},
                            {"description", "description2"},
                            {"severity", "severity2"},
                            {
                                "position", new Dictionary<string, object>
                                {
                                    {"offset", 4L},
                                    {"line", 5L},
                                }
                            }
                        }
                    }
                }
            };
            var collector = new NotificationsCollector();

            collector.Collect(metadata);


            collector.Collected.ShouldBeEquivalentTo(new[]
            {
                new Notification("code1", "title1", "description1",
                    new InputPosition(1, 2, 3), "severity1"),
                new Notification("code2", "title2", "description2",
                    new InputPosition(4, 5, 0), "severity2")
            });
        }
    }
}