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

#if !NET6_0_OR_GREATER
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class TcpSocketClient : ITcpSocketClient
{
    private DriverContext DriverContext { get; }
    private readonly ILogger _logger;

    private Socket _client;

    public TcpSocketClient(DriverContext driverContext, ILogger logger = null)
    {
        DriverContext = driverContext;
        _logger = logger;
    }

    public Stream ReaderStream { get; private set; }
    public Stream WriterStream => ReaderStream;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await ConnectSocketAsync(uri, cancellationToken).ConfigureAwait(false);

        ReaderStream = new NetworkStream(_client);
        if (DriverContext.EncryptionManager.UseTls)
        {
            try
            {
                var sslStream = CreateSecureStream(uri);
                var clientCertificates = await GetClientCertificates().ConfigureAwait(false);
                var protocol = DriverContext.Config.TlsVersion;

                await sslStream
                    .AuthenticateAsClientAsync(uri.Host, clientCertificates, protocol, false)
                    .ConfigureAwait(false);

                ReaderStream = sslStream;
            }
            catch (Exception e)
            {
                throw new ServiceUnavailableException($"Failed to establish encrypted connection with server {uri}.", e);
            }
        }
    }

    public void Dispose()
    {
        if (_client != null)
        {
            if (_client.Connected)
            {
                _client.Shutdown(SocketShutdown.Both);
            }

            _client.Dispose();
            ReaderStream?.Dispose();

            _client = null;
            ReaderStream = null;
        }
    }

    private async ValueTask<X509CertificateCollection> GetClientCertificates()
    {
        if (DriverContext.Config.ClientCertificateProvider == null)
        {
            return null;
        }

        var certificate = await DriverContext.Config.ClientCertificateProvider
            .GetCertificateAsync()
            .ConfigureAwait(false);

        return new X509CertificateCollection(new[] { certificate });
    }

    //Marked as internal for testing purposes.
    private async Task ConnectSocketAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var innerErrors = new List<Exception>();
        var addresses = await DriverContext.HostResolver.ResolveAsync(uri.Host).ConfigureAwait(false);

        foreach (var address in addresses)
        {
            try
            {
                await ConnectSocketAsync(address, uri.Port, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                var exception = e is AggregateException ? e.GetBaseException() : e;

                innerErrors.Add(
                    new IOException(
                        $"Failed to connect to server '{uri}' via IP address '{address}': {exception.Message}",
                        exception));
            }
        }

        // all failed
        throw new IOException(
            $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.",
            new AggregateException(innerErrors));
    }

    internal async Task ConnectSocketAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        InitClient();
        var timeoutValue = DriverContext.Config.ConnectionTimeout;
        using var timeout = new CancellationTokenSource(timeoutValue);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

        try
        {
            await ConnectAsync(address, port, source.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            await TryCleanUpAsync(address, port).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    $"Failed to connect to server {address}:{port} due to acquisition timeout",
                    cancellationToken);
            }

            throw new OperationCanceledException(
                $"Failed to connect to server {address}:{port} within {timeoutValue.TotalMilliseconds}ms.");
        }
        catch
        {
            await TryCleanUpAsync(address, port).ConfigureAwait(false);
            throw;
        }
    }

    private async Task ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        var ctr = cancellationToken.Register(_client.Close);
#if NET6_0_OR_GREATER
        await using var _ = ctr.ConfigureAwait(false);
        await _client.ConnectAsync(new IPEndPoint(address, port), cancellationToken).ConfigureAwait(false);
#else
        using var _ = ctr;
        await _client.ConnectAsync(new IPEndPoint(address, port))
            .Timeout(DriverContext.Config.ConnectionTimeout, cancellationToken)
            .ConfigureAwait(false);
#endif
    }

    private Task TryCleanUpAsync(IPAddress address, int port)
    {
        try
        {
            // close client immediately when failed to connect within timeout
            Dispose();
        }
        catch (Exception e)
        {
            var timeoutValue = DriverContext.Config.ConnectionTimeout;
            _logger.Error(
                e,
                $"Failed to close connect to the server {address}:{port}" +
                $" after connection timed out {timeoutValue.TotalMilliseconds}ms.");
        }

        return Task.CompletedTask;
    }

    private void InitClient()
    {
        var ipv6 = DriverContext.Config.Ipv6Enabled;
        var addressFamily = ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        _client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        if (ipv6)
        {
            _client.DualMode = true;
        }

        _client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            DriverContext.Config.SocketKeepAlive);
    }

    private SslStream CreateSecureStream(Uri uri)
    {
        var negotiator = DriverContext.Config.TlsNegotiator ??
            new DefaultTlsNegotiator(_logger, DriverContext.EncryptionManager);

        return negotiator.NegotiateTls(uri, ReaderStream);
    }
}
