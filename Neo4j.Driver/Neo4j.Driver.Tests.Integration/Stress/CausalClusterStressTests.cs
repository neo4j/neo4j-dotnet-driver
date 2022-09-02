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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public class CausalClusterStressTests : StressTest<CausalClusterStressTests.Context>
    {
        private readonly CausalClusterIntegrationTestFixture _cluster;

        public CausalClusterStressTests(ITestOutputHelper output, CausalClusterIntegrationTestFixture cluster) :
            base(output, cluster.Cluster.BoltRoutingUri, cluster.Cluster.AuthToken, cluster.Cluster.Configure)
        {
            _cluster = cluster;
        }

        protected override Context CreateContext()
        {
            return new Context();
        }

        protected override IEnumerable<IBlockingCommand<Context>> CreateTestSpecificBlockingCommands()
        {
            return new List<IBlockingCommand<Context>>
            {
				new BlockingWriteCommandUsingReadSessionTxFunc<Context>(_driver, false),
				new BlockingWriteCommandUsingReadSessionTxFunc<Context>(_driver, true)
			};
        }

        protected override IEnumerable<IAsyncCommand<Context>> CreateTestSpecificAsyncCommands()
        {
            return new List<IAsyncCommand<Context>>
            {
                new AsyncWriteCommandUsingReadSessionTxFunc<Context>(_driver, false),
                new AsyncWriteCommandUsingReadSessionTxFunc<Context>(_driver, true)
            };
        }

        protected override IEnumerable<IRxCommand<Context>> CreateTestSpecificRxCommands()
        {
            return Enumerable.Empty<IRxCommand<Context>>();
		}

        protected override void PrintStats(Context context)
        {
            _output.WriteLine("{0}", context);
        }

        protected override void VerifyReadQueryDistribution(Context context)
        {
            var clusterAddresses = DiscoverClusterAddresses();

            VerifyServedReadQueries(context, clusterAddresses);
            VerifyServedSimilarAmountOfReadQueries(context, clusterAddresses);
        }
		
		public override bool HandleWriteFailure(Exception error, Context context)
        {
            switch (error)
            {
                case SessionExpiredException _:
                {
                    var isLeaderSwitch = error.Message.EndsWith("no longer accepts writes");
                    if (isLeaderSwitch)
                    {
                        context.LeaderSwitched();
                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        private ClusterAddresses DiscoverClusterAddresses()
        {
            var followers = new List<string>();
            var readReplicas = new List<string>();

            using (var session = _driver.Session())
            {
                var records = session.Run("CALL dbms.cluster.overview()").ToList();

                if (UsingBoltMoreThan5_0())
                {
                    return CreateAutonomousClusterAddresses(records);
                }

                foreach (var record in records)
                {
                    var address = record["addresses"].As<IList<object>>().First().As<string>().Replace("bolt://", "");

                    // Pre 4.0
                    if (record.Keys.Contains("role"))
                    {
                        switch (record["role"].As<string>().ToLowerInvariant())
                        {
                            case "follower":
                                followers.Add(address);
                                break;
                            case "read_replica":
                                readReplicas.Add(address);
                                break;
                        }
                    }

                    // Post 4.0
                    if (record.Keys.Contains("databases"))
                    {
                        switch (record["databases"].As<IDictionary<string, object>>()["neo4j"].As<string>()
                            .ToLowerInvariant())
                        {
                            case "follower":
                                followers.Add(address);
                                break;
                            case "read_replica":
                                readReplicas.Add(address);
                                break;
                        }
                    }
                }
            }

            return new ClusterAddresses(followers, readReplicas);
        }

        private bool UsingBoltMoreThan5_0()
        {
            return _driver.GetServerInfoAsync().GetAwaiter().GetResult().ProtocolVersion
                .Split('.').Take(1).Select(int.Parse).First() >= 5;
        }

        private static ClusterAddresses CreateAutonomousClusterAddresses(List<IRecord> records)
        {
            var neoDbs = records.Select(x =>
                {
                    object role = null;
                    var exists = x.Values.TryGetValue("databases", out var y) && 
                                 y.As<IDictionary<string, object>>().TryGetValue("neo4j", out role);

                    if (!exists)
                        return (address: null, role: null);

                    var address = x.Values["addresses"].As<IList<object>>()
                        .FirstOrDefault()?.As<string>().Replace("bolt://", "");

                    return (address, role: role.As<string>());
                })
                .Where(x => x.role != null)
                .ToList();

            if(neoDbs.Count == 1 && neoDbs[0].role.Equals("standalone", StringComparison.OrdinalIgnoreCase))
                return new ClusterAddresses(Array.Empty<string>(), Array.Empty<string>());

            if (neoDbs.Count > 1)
                return new ClusterAddresses(neoDbs
                    .Where(x => x.role.Equals("follower", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.address).ToList(), Array.Empty<string>());

            throw new Exception("Invalid cluster");
        }

        private static void VerifyServedReadQueries(Context context, ClusterAddresses clusterAddresses)
        {
            foreach (var address in clusterAddresses.Followers)
            {
                context.GetReadQueries(address).Should()
                    .BePositive("Follower {0} did not serve any read queries", address);
            }

            foreach (var address in clusterAddresses.ReadReplicas)
            {
                context.GetReadQueries(address).Should()
                    .BePositive("Read replica {0} did not serve any read queries", address);
            }
        }

        private static void VerifyServedSimilarAmountOfReadQueries(Context context, ClusterAddresses clusterAddresses)
        {
            void Verify(string serverType, IEnumerable<string> addresses)
            {
                var expectedMagnitude = -1;
                foreach (var address in addresses)
                {
                    var queries = context.GetReadQueries(address);
                    var orderOfMagnitude = GetOrderOfMagnitude(queries);
                    if (expectedMagnitude == -1)
                    {
                        expectedMagnitude = orderOfMagnitude;
                    }

                    orderOfMagnitude.Should().BeInRange(expectedMagnitude - 1, expectedMagnitude + 1,
                        "{0} {1} is expected to server similar amount of queries. Context: {2}.", serverType, address,
                        context);
                }
            }

            Verify("Follower", clusterAddresses.Followers);
            Verify("Read-replica", clusterAddresses.ReadReplicas);
        }

        private static int GetOrderOfMagnitude(long number)
        {
            var result = 1;
            while (number >= 10)
            {
                number /= 10;
                result++;
            }

            return result;
        }

        public class Context : StressTestContext
        {
            private readonly ConcurrentDictionary<string, AtomicLong> _readQueriesByServer = new
                ConcurrentDictionary<string, AtomicLong>();

            private long _leaderSwitches;

            protected override void ProcessSummary(IResultSummary summary)
            {
                if (summary == null)
                {
                    return;
                }

                _readQueriesByServer.AddOrUpdate(summary.Server.Address, new AtomicLong(1),
                    (key, value) => value.Increment());
            }

            public long GetReadQueries(string address)
            {
                return _readQueriesByServer.TryGetValue(address, out var value) ? value.Value : 0;
            }

            public long LeaderSwitches => Interlocked.Read(ref _leaderSwitches);

            public void LeaderSwitched()
            {
                Interlocked.Increment(ref _leaderSwitches);
            }

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("CausalClusterContext{")
                    .AppendFormat("Bookmark={0}, ", Bookmarks)
                    .AppendFormat("BookmarkFailures={0}, ", BookmarkFailures)
                    .AppendFormat("NodesCreated={0}, ", CreatedNodesCount)
                    .AppendFormat("NodesRead={0}, ", ReadNodesCount)
                    .AppendFormat("LeaderSwitches={0}, ", LeaderSwitches)
                    .AppendFormat("ReadsByServers={0}", _readQueriesByServer.ToContentString())
                    .Append("}")
                    .ToString();
            }
        }

        public class AtomicLong
        {
            private long _value;

            public AtomicLong(long value)
            {
                _value = value;
            }

            public long Value => Interlocked.Read(ref _value);

            public AtomicLong Increment()
            {
                Interlocked.Increment(ref _value);
                return this;
            }

            public AtomicLong Decrement()
            {
                Interlocked.Decrement(ref _value);
                return this;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private class ClusterAddresses
        {
            public ClusterAddresses(IEnumerable<string> followers, IEnumerable<string> readReplicas)
            {
                Followers = followers.ToHashSet();
                ReadReplicas = readReplicas.ToHashSet();
            }

            public ISet<string> Followers { get; }

            public ISet<string> ReadReplicas { get; }

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("ClusterAddresses{")
                    .AppendFormat("Followers={0}, ", Followers)
                    .AppendFormat("Read-Replicas={0}", ReadReplicas)
                    .Append("}")
                    .ToString();
            }
        }
    }
}