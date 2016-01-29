//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests.Result
{
    public class ResultBuilderTests
    {
        public class CollectMetaMethod
        {
            [Fact]
            public void ShouldCollectKeys()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"fields", new List<object> {"fieldKey1", "fieldKey2", "fieldKey3"} },{"type", "r" } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                result.Keys.Should().ContainInOrder("fieldKey1", "fieldKey2", "fieldKey3");
            }

            [Fact]
            public void ShouldCollectType()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                result.Summarize().StatementType.Should().Be(StatementType.ReadOnly);
            }

            [Fact]
            public void ShouldCollectStattistics()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" }, {"stats", new Dictionary<string, object> { {"nodes-created", 10L}, {"nodes-deleted", 5L} } } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                var statistics = result.Summarize().UpdateStatistics;
                statistics.NodesCreated.Should().Be(10);

            }

            [Fact]
            public void ShouldCollectNotifications()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"type", "r" },
                    {
                        "notifications", new List<object>
                        {
                            new Dictionary<string, object> {{"code", "CODE"}, {"title", "TITLE"}},
                            new Dictionary<string, object>
                            {
                                {"description", "DES"},
                                {
                                    "position", new Dictionary<string, object>
                                    {
                                        {"offset", 11L}
                                    }
                                }
                            }
                        }
                    }
                };

                builder.CollectMeta(meta);

                InputPosition position = new InputPosition(0,0,0);

                var result = builder.Build();
                var notifications = result.Summarize().Notifications;
                notifications.Should().HaveCount(2);
                notifications[0].Code.Should().Be("CODE");
                notifications[0].Title.Should().Be("TITLE");
                notifications[0].Position.Offset.Should().Be(0);
                notifications[0].Position.Column.Should().Be(0);
                notifications[0].Position.Line.Should().Be(0);
                notifications[0].Description.Should().BeEmpty();

                notifications[1].Description.Should().Be("DES");
                notifications[1].Code.Should().BeEmpty();
                notifications[1].Title.Should().BeEmpty();
                notifications[1].Position.Offset.Should().Be(11);
                notifications[1].Position.Column.Should().Be(0);
                notifications[1].Position.Line.Should().Be(0);
            }

            [Fact]
            public void ShouldCollectSimplePlan()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                {   {"type", "r" },
                    { "plan", new Dictionary<string, object>
                {
                    {"operatorType", "X"}
                } } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                var plan = result.Summarize().Plan;
                plan.OperatorType.Should().Be("X");
                plan.Arguments.Should().BeEmpty();
                plan.Children.Should().BeEmpty();
                plan.Identifiers.Should().BeEmpty();

            }

            [Fact]
            public void ShouldCollectPlanThatContainsPlans()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" }, {"plan", new Dictionary<string, object>
                {
                    {"operatorType", "X"},
                    { "args", new Dictionary<string, object> { {"a", 1}, {"b", "lala"} } },
                    { "identifiers", new List<object> {"id1", "id2"} },
                    { "children", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "operatorType", "tt"},
                            { "children", new List<object>()}
                        },
                        new Dictionary<string, object>
                        {
                            { "children", new List<object>
                            {
                               new Dictionary<string, object> { { "operatorType", "Y"} }
                            } }
                        }
                        }
                    }
                } } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                var plan = result.Summarize().Plan;
                plan.OperatorType.Should().Be("X");
                plan.Arguments.Should().ContainKey("a");
                plan.Arguments["a"].Should().Be(1);
                plan.Arguments.Should().ContainKey("b");
                plan.Arguments["b"].Should().Be("lala");
                plan.Identifiers.Should().ContainInOrder("id1", "id2");
                plan.Children.Should().NotBeNull();
                var children = plan.Children;
                children.Should().HaveCount(2);
                children[0].OperatorType.Should().Be("tt");
                children[0].Children.Should().BeEmpty();
                children[1].Children.Should().HaveCount(1);
                children[1].Children[0].OperatorType.Should().Be("Y");
            }

            [Fact]
            public void ShouldCollectProfiledPlanThatContainsProfiledPlans()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" }, {"profile", new Dictionary<string, object>
                {
                    {"operatorType", "X"},
                    { "args", new Dictionary<string, object> { {"a", 1}, {"b", "lala"} } },
                    { "identifiers", new List<object> {"id1", "id2"} },
                    { "children", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "operatorType", "tt"},
                            { "children", new List<object>()}
                        },
                        new Dictionary<string, object>
                        {
                            { "children", new List<object>
                            {
                               new Dictionary<string, object> { { "operatorType", "Y"} }
                            } }
                        }
                        }
                    }
                } } };
                builder.CollectMeta(meta);

                var result = builder.Build();
                var profile = result.Summarize().Profile;
                profile.DbHits.Should().Be(0L);
                profile.OperatorType.Should().Be("X");
                profile.Arguments.Should().ContainKey("a");
                profile.Arguments["a"].Should().Be(1);
                profile.Arguments.Should().ContainKey("b");
                profile.Arguments["b"].Should().Be("lala");
                profile.Identifiers.Should().ContainInOrder("id1", "id2");
                profile.Children.Should().NotBeNull();
                var children = profile.Children;
                children.Should().HaveCount(2);
                children[0].OperatorType.Should().Be("tt");
                children[0].Children.Should().BeEmpty();
                children[1].Children.Should().HaveCount(1);
                children[1].Children[0].OperatorType.Should().Be("Y");
            }
        }
    }
}
