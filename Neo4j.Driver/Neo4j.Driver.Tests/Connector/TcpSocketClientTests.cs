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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Connector;
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

    public class SystemReportsDeadMethod
    {
        [Fact]
        public async Task ShouldReportAliveWhileThePeerHoldsTheConnectionOpen()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var client = await ConnectedClientAsync(listener);
                using var accepted = await listener.AcceptSocketAsync();

                var reportedDead = client.SystemReportsDead();

                reportedDead.Should().BeFalse();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ShouldReportDeadAfterThePeerClosesGracefully()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var client = await ConnectedClientAsync(listener);
                var accepted = await listener.AcceptSocketAsync();

                accepted.Shutdown(SocketShutdown.Both);
                accepted.Close();

                var reportedDead = WaitForReportedDead(client);

                reportedDead.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ShouldReportDeadAfterThePeerAbortsTheConnection()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var client = await ConnectedClientAsync(listener);
                var accepted = await listener.AcceptSocketAsync();

                accepted.LingerState = new LingerOption(true, 0);
                accepted.Close();

                var reportedDead = WaitForReportedDead(client);

                reportedDead.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ShouldReportDeadWhenThePeerClosesAfterLeavingUnreadBytes()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var client = await ConnectedClientAsync(listener);
                var accepted = await listener.AcceptSocketAsync();

                await accepted.SendAsync(new byte[] { 0x15, 0x03, 0x03, 0x00, 0x02 }, SocketFlags.None);
                accepted.Shutdown(SocketShutdown.Both);
                accepted.Close();

                var reportedDead = WaitForReportedDead(client);

                reportedDead.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ShouldReportDeadWhenThePeerSendsUnsolicitedBytes()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var client = await ConnectedClientAsync(listener);
                using var accepted = await listener.AcceptSocketAsync();

                await accepted.SendAsync(new byte[] { 0x17, 0x03, 0x03 }, SocketFlags.None);

                var reportedDead = WaitForReportedDead(client);

                reportedDead.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task<TcpSocketClient> ConnectedClientAsync(TcpListener listener)
        {
            var client = new TcpSocketClient(
                new DriverContext(
                    new Uri("bolt://localhost:7687"),
                    new StaticAuthTokenManager(AuthTokens.None),
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(10) }));

            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            await client.ConnectSocketAsync(IPAddress.Loopback, port);

            return client;
        }

        private bool WaitForReportedDead(TcpSocketClient client)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                if (client.SystemReportsDead())
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            return false;
        }
    }

    public class SystemReportsDeadMethodOverTls
    {
        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public async Task ShouldReportAliveAfterARoundTripOnAnEncryptedConnection(SslProtocols protocol)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var (client, server) = await ConnectedEncryptedPairAsync(listener, protocol);

                await client.WriterStream.WriteAsync(new byte[] { 1 });
                await client.WriterStream.FlushAsync();
                await server.ReadAsync(new byte[1]);
                await server.WriteAsync(new byte[] { 2 });
                await server.FlushAsync();
                await client.ReaderStream.ReadAsync(new byte[1]);

                var reportedDead = client.SystemReportsDead();

                reportedDead.Should().BeFalse();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls13)]
        public async Task ShouldReportDeadWhenThePeerSendsCloseNotifyAndKeepsTheSocketOpen(SslProtocols protocol)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var (client, server) = await ConnectedEncryptedPairAsync(listener, protocol);

                await server.ShutdownAsync();

                var reportedDead = WaitForReportedDead(client);

                reportedDead.Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task<(TcpSocketClient Client, SslStream Server)> ConnectedEncryptedPairAsync(
            TcpListener listener,
            SslProtocols protocol)
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var client = new TcpSocketClient(
                new DriverContext(
                    new Uri($"bolt+ssc://127.0.0.1:{port}"),
                    new StaticAuthTokenManager(AuthTokens.None),
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(10), TlsVersion = protocol }));

            var accepting = listener.AcceptSocketAsync();
            var connecting = client.ConnectAsync(new Uri($"bolt+ssc://127.0.0.1:{port}"));
            var accepted = await accepting;
            var server = new SslStream(new NetworkStream(accepted, true));
            var serverAuthenticating = server.AuthenticateAsServerAsync(
                new SslServerAuthenticationOptions
                {
                    ServerCertificate = SelfSignedCertificate(),
                    EnabledSslProtocols = protocol
                });

            try
            {
                await connecting;
            }
            catch (ServiceUnavailableException e) when (e.GetBaseException() is PlatformNotSupportedException)
            {
                server.Dispose();
                Assert.Skip($"{protocol} is not supported on this platform");
            }

            await serverAuthenticating;

            return (client, server);
        }

        private static X509Certificate2 SelfSignedCertificate()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            var pfx = certificate.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(pfx, null);
#else
            return new X509Certificate2(pfx);
#endif
        }

        private static bool WaitForReportedDead(TcpSocketClient client)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                if (client.SystemReportsDead())
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            return false;
        }
    }
}
