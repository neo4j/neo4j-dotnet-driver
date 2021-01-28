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
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Reactive;
using Neo4j.Driver.Tests;
using Xunit;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.Simple.Internal
{
    public static class InternalTransactionTests
    {
        public class TransactionConfig
        {
            [Fact]
            public void ShouldDelegateToAsyncTransaction()
            {
                var asyncTx = new Mock<IInternalAsyncTransaction>();
                var tx = new InternalTransaction(asyncTx.Object, Mock.Of<BlockingExecutor>());

                var config = tx.TransactionConfig;

                asyncTx.Verify(x => x.TransactionConfig, Times.Once);
            }
        }
    }
}