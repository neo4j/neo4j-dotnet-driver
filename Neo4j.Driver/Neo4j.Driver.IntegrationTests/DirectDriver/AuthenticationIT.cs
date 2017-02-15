using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class AuthenticationIT : DirectDriverIT
    {
        public AuthenticationIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        { }

        [Fact]
        public void AuthenticationErrorIfWrongAuthToken()
        {
            Exception exception;
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthTokens.Basic("fake", "fake")))
            {
                exception = Record.Exception(()=>driver.Session());
            }
            exception.Should().BeOfType<AuthenticationException>();
            exception.Message.Should().Contain("The client is unauthorized due to authentication failure.");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
