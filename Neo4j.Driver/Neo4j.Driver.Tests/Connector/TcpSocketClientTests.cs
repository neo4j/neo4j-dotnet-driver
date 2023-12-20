// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Auth;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class TcpSocketClientTests
{
    public class ConnectSocketAsyncMethod
    {
        [Fact]
        public async Task ShouldThrowExceptionIfConnectionTimedOut()
        {
            var client = new TcpSocketClient(
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    new StaticAuthTokenManager(AuthTokens.None),
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(1) }));

            // use non-routable IP address to mimic a connect timeout
            // https://stackoverflow.com/questions/100841/artificially-create-a-connection-timeout-error
            var exception = await Record.ExceptionAsync(
                () =>
                    client.ConnectSocketAsync(
                        IPAddress.Parse("192.168.0.0"),
                        9999));

            exception
                .Should()
                .NotBeNull()
                .And
                .BeOfType<OperationCanceledException>()
                .Which
                .Message
                .Should()
                .Be("Failed to connect to server 192.168.0.0:9999 within 1000ms.");
        }

        [Fact]
        public async Task ShouldBeAbleToConnectAgainIfFirstFailed()
        {
            var client = new TcpSocketClient(
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    new StaticAuthTokenManager(AuthTokens.None),
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(10) }));

            // We fail to connect the first time as there is no server to connect to
            // ReSharper disable once PossibleNullReferenceException
            var exception = await Record.ExceptionAsync(
                async () => await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 54321));
            // start a server on port 20003

            var serverSocket = new TcpListener(new IPEndPoint(IPAddress.Loopback, 54321));
            try
            {
                serverSocket.Start();

                // We should not get any error this time as server in online now.
                await client.ConnectSocketAsync(IPAddress.Parse("127.0.0.1"), 54321);
            }
            finally
            {
                serverSocket.Stop();
            }
        }
    }

    public class ConnectAsyncMethod
    {
        [Fact]
        public async Task ShouldThrowExceptionIfConnectionTimedOut()
        {
            var client = new TcpSocketClient(
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    AuthTokenManagers.None,
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(1) }));

            // ReSharper disable once PossibleNullReferenceException
            // use non-routable IP address to mimic a connect timeout
            // https://stackoverflow.com/questions/100841/artificially-create-a-connection-timeout-error
            var exception =
                await Record.ExceptionAsync(() => client.ConnectAsync(new Uri("bolt://192.168.0.0:9998")));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<IOException>();
            exception.Message.Should()
                .Be(
                    "Failed to connect to server 'bolt://192.168.0.0:9998/' via IP addresses'[192.168.0.0]' at port '9998'.");

            var baseException = exception.GetBaseException();
            baseException.Should().BeOfType<OperationCanceledException>(exception.ToString());
            baseException.Message.Should().Be("Failed to connect to server 192.168.0.0:9998 within 1000ms.");
        }
    }
}
