﻿// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stress
{
	public class AsyncWriteCommandUsingReadSessionTxFunc<TContext> : AsyncCommand<TContext>
		where TContext : StressTestContext
	{
		public AsyncWriteCommandUsingReadSessionTxFunc(IDriver driver, bool useBookmark)
			: base(driver, useBookmark)
		{
		}

		public override async Task ExecuteAsync(TContext context)
		{
			var cursor = default(IResultCursor);
			
			var session = NewSession(AccessMode.Read, context);
			try
			{
				await session.ReadTransactionAsync(async tx =>
				{
					try
					{
						var exc = await Record.ExceptionAsync(async () =>
						{
							cursor = await tx.RunAsync("CREATE ()");
							await cursor.ConsumeAsync();
						});

						exc.Should().BeOfType<ClientException>();
					}
					finally
					{
						await tx.RollbackAsync().ConfigureAwait(false);
					}

				}).ConfigureAwait(false);
			}
			finally
			{
				await session.CloseAsync().ConfigureAwait(false);
			}

			cursor.Should().NotBeNull();
			var summary = await cursor.ConsumeAsync();
			summary.Counters.NodesCreated.Should().Be(0);
		}
	}
}