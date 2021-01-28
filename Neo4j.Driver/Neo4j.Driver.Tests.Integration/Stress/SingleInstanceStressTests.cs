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
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Stress
{
    [Collection(SAIntegrationCollection.CollectionName)]
    public class SingleInstanceStressTests : StressTest<SingleInstanceStressTests.Context>
    {
        private readonly StandAloneIntegrationTestFixture _standalone;

        public SingleInstanceStressTests(ITestOutputHelper output, StandAloneIntegrationTestFixture standalone) :
            base(output, standalone.StandAloneSharedInstance.BoltUri, standalone.StandAloneSharedInstance.AuthToken)
        {
            _standalone = standalone;
        }

        protected override Context CreateContext()
        {
            return new Context(_standalone.StandAloneSharedInstance.BoltUri.Authority);
        }

        protected override IEnumerable<IBlockingCommand<Context>> CreateTestSpecificBlockingCommands()
        {
            return Enumerable.Empty<IBlockingCommand<Context>>();
        }

        protected override IEnumerable<IAsyncCommand<Context>> CreateTestSpecificAsyncCommands()
        {
            return Enumerable.Empty<IAsyncCommand<Context>>();
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
            context.ReadNodesCount.Should().BePositive();
        }

        public override bool HandleWriteFailure(Exception error, Context context)
        {
            return false;
        }

        public class Context : StressTestContext
        {
            private readonly string _expectedAddress;
            private long _readQueries;

            public Context(string expectedAddress)
            {
                _expectedAddress = expectedAddress;
            }

            public long ReadQueries => Interlocked.Read(ref _readQueries);

            protected override void ProcessSummary(IResultSummary summary)
            {
                if (summary == null)
                {
                    return;
                }

                summary.Server.Address.Should().Be(_expectedAddress);
                Interlocked.Increment(ref _readQueries);
            }

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("SingleInstanceContext{")
                    .AppendFormat("Bookmark={0}, ", Bookmark)
                    .AppendFormat("BookmarkFailures={0}, ", BookmarkFailures)
                    .AppendFormat("NodesCreated={0}, ", CreatedNodesCount)
                    .AppendFormat("NodesRead={0}", ReadNodesCount)
                    .Append("}")
                    .ToString();
            }
        }
    }
}