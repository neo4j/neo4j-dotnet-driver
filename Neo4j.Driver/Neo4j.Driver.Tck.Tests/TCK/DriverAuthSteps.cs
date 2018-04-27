// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;
using Xunit;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class DriverAuthSteps
    {
        [Given(@"a driver is configured with auth enabled and correct password is provided")]
        public void GivenADriverIsConfiguredWithAuthEnabledAndCorrectPasswordIsProvided()
        {
            var driverWithCorrectPassword = GraphDatabase.Driver(TckHooks.Uri, TckHooks.AuthToken);
            ScenarioContext.Current.Set(driverWithCorrectPassword);
        }
        
        [Given(@"a driver is configured with auth enabled and the wrong password is provided")]
        public void GivenADriverIsConfiguredWithAuthEnabledAndTheWrongPasswordIsProvided()
        {
            var driverWithIncorrectPassword = GraphDatabase.Driver(TckHooks.Uri, AuthTokens.Basic("neo4j", "lala"));
            ScenarioContext.Current.Set(driverWithIncorrectPassword);
        }
        
        [Then(@"reading and writing to the database should be possible")]
        public void ThenReadingAndWritingToTheDatabaseShouldBePossible()
        {
            var driver = ScenarioContext.Current.Get<IDriver>();
            using (driver)
            using (var session = driver.Session())
            {
                var result = session.Run("CREATE () RETURN 2 as Number");
                result.Peek();
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
                result.Single()["Number"].ValueAs<int>().Should().Be(2);
            }
        }

        [Then(@"reading and writing to the database should not be possible")]
        public void ThenReadingAndWritingToTheDatabaseShouldNotBePossible()
        {
            var driver = ScenarioContext.Current.Get<IDriver>();
            using (driver)
            using (var session = driver.Session())
            {
                var exception = Record.Exception(() => session.Run("RETURN 1"));
                exception.Should().BeOfType<AuthenticationException>();
                exception.Message.Should().StartWith("The client is unauthorized due to authentication failure");
            }
        }
        
        [Then(@"a `Protocol Error` is raised")]
        public void ThenAProtocolErrorIsRaised()
        {
            // the check is done in ThenReadingAndWritingToTheDatabaseShouldNotBePossible
        }
    }
}
