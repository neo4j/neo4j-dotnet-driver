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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class StatementRunnerTests
    {
        internal class MyStatementRunner : StatementRunner
        {
            public string Statement { private set; get; }
            public IDictionary<string, object> Parameters { private set; get; } 

            public override IStatementResult Run(Statement statement)
            {
                Statement = statement.Text;
                Parameters = statement.Parameters;
                return null; // nah, I do not care
            }

            public override Task<IStatementResultCursor> RunAsync(Statement statement)
            {
                throw new System.NotImplementedException();
            }

            protected override void Dispose(bool isDisposing)
            {
                if (!isDisposing)
                {
                    return;
                }
                Dispose();
            }
        }

        public class RunStatementMethod
        {
            [Fact]
            public void ShouldInvokeRunStringMethod()
            {
                var stRunner = new MyStatementRunner();
                stRunner.Run(new Statement("lalalala"));
                stRunner.Statement.Should().Be("lalalala");
                stRunner.Parameters.Should().BeEmpty();
            }

            [Fact]
            public void ShouldInvokeRunStringMethodWithParameters()
            {
                var stRunner = new MyStatementRunner();
                stRunner.Run("buibuibui", new {Age = 20, Name = "buibuibui", Hobby = new []{"painting", "skiing"}});
                stRunner.Statement.Should().Be("buibuibui");

                var parameters = stRunner.Parameters;

                parameters.Count.Should().Be(3);
                parameters["Age"].Should().Be(20);
                parameters["Name"].Should().Be("buibuibui");

                var hobby = parameters["Hobby"].ValueAs<List<string>>();
                hobby.Count.Should().Be(2);
                hobby.Contains("painting").Should().BeTrue();
                hobby.Contains("skiing").Should().BeTrue();
            }
        }
    }
}
