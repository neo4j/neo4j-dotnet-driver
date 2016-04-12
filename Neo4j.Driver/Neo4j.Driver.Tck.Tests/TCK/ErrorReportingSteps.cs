using System;
using System.Net.Sockets;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class ErrorReportingSteps : TckStepsBase
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
            using (var session = Driver.Session())
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
            using (var session = Driver.Session())
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
            using (var session = Driver.Session())
            {
                var ex = Xunit.Record.Exception(() => session.Run("Invalid Cypher"));
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
            }
        }
        
        [When(@"I set up a driver to an incorrect port")]
        public void WhenISetUpADriverToAnIncorrectPort()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:1234"))
            {
                var ex = Xunit.Record.Exception(() => driver.Session().Run("Return 1"));
                ex.Should().BeOfType<AggregateException>();
                ex = ex.GetBaseException();
                ex.Should().BeOfType<SocketException>();
                ex.Message.Should().Be("No connection could be made because the target machine actively refused it 127.0.0.1:1234");
            }
        }

        [When(@"I set up a driver with wrong scheme")]
        public void WhenISetUpADriverWithWrongScheme()
        {
            using (var driver = GraphDatabase.Driver("http://localhost"))
            {
                var ex = Xunit.Record.Exception(() => driver.Session().Run("RETURN 1"));
                ex.Should().BeOfType<NotSupportedException>();
                ex.Message.Should().Be("Unsupported protocol: http");
            }
        }

        [Then(@"it throws a `ClientException`")]
        public void ThenItThrowsAClientException(Table table)
        {
        }
       
    }
}
