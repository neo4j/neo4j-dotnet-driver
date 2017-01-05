// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Net.Sockets;
using FluentAssertions;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class ErrorReportingSteps
    {
        [Given(@"I have a driver")]
        public void GivenIHaveADriver()
        {
        }

        [When(@"I start a `Transaction` through a session")]
        public void WhenIStartATransactionThroughASession()
        {
        }

        [When(@"`run` a query with that same session without closing the transaction first")]
        public void WhenRunAQueryWithThatSameSessionWithoutClosingTheTransactionFirst()
        {
            using (var session = TckHooks.CreateSelfManagedSession())
            using (var tx = session.BeginTransaction())
            {
                var ex = Xunit.Record.Exception(() => session.Run("RETURN 1"));
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Please close the currently open transaction object");
            }
        }
        
        [When(@"I start a new `Transaction` with the same session before closing the previous")]
        public void WhenIStartANewTransactionWithTheSameSessionBeforeClosingThePrevious()
        {
            using (var session = TckHooks.CreateSelfManagedSession())
            using (var tx = session.BeginTransaction())
            {
                var ex = Xunit.Record.Exception(() => session.BeginTransaction());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Please close the currently open transaction object");
            }
        }
        
        [When(@"I run a non valid cypher statement")]
        public void WhenIRunANonValidCypherStatement()
        {
            using (var session = TckHooks.CreateSelfManagedSession())
            {
                var result = session.Run("Invalid Cypher");
                var ex = Xunit.Record.Exception(() => result.Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
            }
        }
        
        [When(@"I set up a driver to an incorrect port")]
        public void WhenISetUpADriverToAnIncorrectPort()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:1234"))
            {
                var ex = Xunit.Record.Exception(() => driver.Session());
                ex.Should().BeOfType<AggregateException>();
                ex = ex.GetBaseException();
                ex.Should().BeOfType<SocketException>();
                ex.Message.Should().Be("No connection could be made because the target machine actively refused it 127.0.0.1:1234");
            }
        }

        [When(@"I set up a driver with wrong scheme")]
        public void WhenISetUpADriverWithWrongScheme()
        {
            var ex = Xunit.Record.Exception(() => GraphDatabase.Driver("http://localhost"));
            ex.Should().BeOfType<NotSupportedException>();
            ex.Message.Should().Be("Unsupported URI scheme: http");
        }

        [Then(@"it throws a `ClientException`")]
        public void ThenItThrowsAClientException(Table table)
        {
        }
       
    }
}
