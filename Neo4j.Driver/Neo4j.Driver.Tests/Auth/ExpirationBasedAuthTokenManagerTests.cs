// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Services;
using Xunit;

namespace Neo4j.Driver.Tests.Auth
{
    public class ExpirationBasedAuthTokenManagerTests
    {
        [Fact]
        public async Task ShouldRequestToken()
        {
            var basicToken = AuthTokens.Basic("uid", "pwd");
            var authData = new AuthTokenAndExpiration(basicToken, new DateTime(2023, 02, 28));
            Task<AuthTokenAndExpiration> GetToken()
            {
                return Task.FromResult(authData);
            }

            var subject = new ExpirationBasedAuthTokenManager(GetToken);

            var returnedToken = await subject.GetTokenAsync();
            returnedToken.Should().Be(basicToken);
        }

        [Fact]
        public async Task ShouldCacheToken()
        {
            var basicToken = AuthTokens.Basic("uid", "pwd");
            var authData = new AuthTokenAndExpiration(
                basicToken,
                new DateTime(2023, 02, 28, 15, 0, 0)); // expires at 3pm

            int callCount = 0;
            Task<AuthTokenAndExpiration> GetToken()
            {
                callCount++;
                return Task.FromResult(authData);
            }

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider
                .Setup(x => x.Now())
                .Returns(new DateTime(2023, 02, 28, 10, 0, 0)); // it is currently 10am

            var subject = new ExpirationBasedAuthTokenManager(dateTimeProvider.Object, GetToken);

            // call twice
            await subject.GetTokenAsync();
            var returnedToken = await subject.GetTokenAsync();

            returnedToken.Should().Be(basicToken);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ShouldRenewTokenAfterExpiryTime()
        {
            var firstToken = AuthTokens.Basic("first", "token");
            var firstAuthData = new AuthTokenAndExpiration(
                firstToken,
                new DateTime(2023, 02, 28, 15, 0, 0)); // expires at 3pm

            var secondToken = AuthTokens.Basic("second", "token");
            var secondAuthData = new AuthTokenAndExpiration(
                secondToken,
                new DateTime(2023, 02, 28, 16, 0, 0)); // expires at 4pm

            int callCount = 0;
            Task<AuthTokenAndExpiration> GetToken()
            {
                if (callCount == 0)
                {
                    callCount++;
                    return Task.FromResult(firstAuthData);
                }

                return Task.FromResult(secondAuthData);
            }

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            bool dateRequested = false;
            dateTimeProvider
                .Setup(x => x.Now())
                .Returns(() =>
                    {
                        if (!dateRequested)
                        {
                            dateRequested = true;
                            return new DateTime(2023, 02, 28, 10, 0, 0); // first time, 10am
                        }

                        return new DateTime(2023, 02, 28, 16, 0, 0); // after that, 4pm (expired)
                    });

            var subject = new ExpirationBasedAuthTokenManager(dateTimeProvider.Object, GetToken);
            var firstReturnedToken = await subject.GetTokenAsync();
            await subject.GetTokenAsync(); // do a couple more times so that a new one should be requested
            await subject.GetTokenAsync();
            var secondReturnedToken = await subject.GetTokenAsync();

            firstReturnedToken.Should().Be(firstToken);
            secondReturnedToken.Should().Be(secondToken);
        }

        [Fact]
        public async Task ShouldRefreshTokenOnExpiry()
        {
            var firstToken = AuthTokens.Basic("first", "token");
            var firstAuthData = new AuthTokenAndExpiration(
                firstToken,
                new DateTime(2023, 02, 28, 15, 0, 0)); // expires at 3pm

            var secondToken = AuthTokens.Basic("second", "token");
            var secondAuthData = new AuthTokenAndExpiration(
                secondToken,
                new DateTime(2023, 02, 28, 16, 0, 0)); // expires at 4pm

            int callCount = 0;

            Task<AuthTokenAndExpiration> GetToken()
            {
                if (callCount == 0)
                {
                    callCount++;
                    return Task.FromResult(firstAuthData);
                }

                return Task.FromResult(secondAuthData);
            }

            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider
                .Setup(x => x.Now())
                .Returns(new DateTime(2023, 01, 01, 09, 00, 00)); // before expiry of first token

            var subject = new ExpirationBasedAuthTokenManager(dateTimeProvider.Object, GetToken);
            var firstReturnedToken = await subject.GetTokenAsync();
            await subject.OnTokenExpiredAsync(firstReturnedToken);
            var secondReturnedToken = await subject.GetTokenAsync();

            secondReturnedToken.Should().Be(secondToken);
        }
    }
}
