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
using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class AuthenticationIT : DirectDriverTestBase
    {
        public AuthenticationIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        { }

        [RequireServerFact]
        public void AuthenticationErrorIfWrongAuthToken()
        {
            Exception exception;
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthTokens.Basic("fake", "fake")))
            using (var session = driver.Session())
            {
                exception = Record.Exception(() =>session.Run("Return 1"));
            }
            exception.Should().BeOfType<AuthenticationException>();
            exception.Message.Should().Contain("The client is unauthorized due to authentication failure.");
        }

        [RequireServerFact]
        public void ShouldProvideRealmWithBasicAuthToken()
        {
            var oldAuthToken = AuthToken.AsDictionary();
            var newAuthToken = AuthTokens.Basic(oldAuthToken["principal"].ValueAs<string>(),
                oldAuthToken["credentials"].ValueAs<string>(), "native");

            using (var driver = GraphDatabase.Driver(ServerEndPoint, newAuthToken))
            using (var session = driver.Session())
            {
                var result = session.Run("RETURN 2 as Number");
                result.Consume();
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
            }
        }

        [RequireServerFact]
        public void ShouldCreateCustomAuthToken()
        {
            var oldAuthToken = AuthToken.AsDictionary();
            var newAuthToken = AuthTokens.Custom(
                oldAuthToken["principal"].ValueAs<string>(),
                oldAuthToken["credentials"].ValueAs<string>(),
                "native",
                "basic");

            using (var driver = GraphDatabase.Driver(ServerEndPoint, newAuthToken))
            using (var session = driver.Session())
            {
                var result = session.Run("RETURN 2 as Number");
                result.Consume();
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
            }
        }

        [RequireServerFact]
        public void ShouldCreateCustomAuthTokenWithAdditionalParameters()
        {
            var oldAuthToken = AuthToken.AsDictionary();
            var newAuthToken = AuthTokens.Custom(
                oldAuthToken["principal"].ValueAs<string>(),
                oldAuthToken["credentials"].ValueAs<string>(),
                "native",
                "basic",
                new Dictionary<string, object> { { "secret", 42 } });

            using (var driver = GraphDatabase.Driver(ServerEndPoint, newAuthToken))
            using (var session = driver.Session())
            {
                var result = session.Run("RETURN 2 as Number");
                result.Consume();
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
            }
        }
    }
}
