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

#if NET6_0_OR_GREATER
#else
using Neo4j.Driver.Internal.Extensions;
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
using static System.Security.Authentication.SslProtocols;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class TcpSocketClient : ITcpSocketClient
{
    private readonly TimeSpan _connectionTimeout;
    private readonly EncryptionManager _encryptionManager;
    private readonly bool _ipv6Enabled;
    private readonly ILogger _logger;

    private readonly IHostResolver _resolver;
    private readonly bool _socketKeepAliveEnabled;
    private Socket _client;

    public TcpSocketClient(SocketSettings socketSettings, ILogger logger = null)
    {
        if (socketSettings == null)
        {
            throw new ArgumentNullException(nameof(socketSettings));
        }

        _resolver = socketSettings.HostResolver;
        _encryptionManager = socketSettings.EncryptionManager;

        _ipv6Enabled = socketSettings.Ipv6Enabled;
        _connectionTimeout = socketSettings.ConnectionTimeout;
        _socketKeepAliveEnabled = socketSettings.SocketKeepAliveEnabled;

        _logger = logger;
    }

    public Stream ReaderStream { get; private set; }

    public Stream WriterStream => ReaderStream;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await ConnectSocketAsync(uri, cancellationToken).ConfigureAwait(false);

        ReaderStream = new NetworkStream(_client);
        if (_encryptionManager.UseTls)
        {
            try
            {
                var sslStream = CreateSecureStream(uri);

                await sslStream
                    .AuthenticateAsClientAsync(uri.Host, null, Tls12, false)
                    .ConfigureAwait(false);

                ReaderStream = sslStream;
            }
            catch (Exception e)
            {
                throw new SecurityException($"Failed to establish encrypted connection with server {uri}.", e);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            if (_client.Connected)
            {
                _client.Shutdown(SocketShutdown.Both);
            }

            _client.Dispose();

            _client = null;
            ReaderStream = null;
        }

        #if NET6_0_OR_GREATER
        return ValueTask.CompletedTask;
        #else
        return default;
        #endif
    }

    private async Task ConnectSocketAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var innerErrors = new List<Exception>();
        var addresses = await _resolver.ResolveAsync(uri.Host).ConfigureAwait(false);

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
        using var timeout = new CancellationTokenSource(_connectionTimeout);
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
                $"Failed to connect to server {address}:{port} within {_connectionTimeout.TotalMilliseconds}ms.");
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
        try
        {
            await _client.ConnectAsync(new IPEndPoint(address, port))
                .Timeout(_connectionTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ctr.Dispose();
        }
        #endif
    }

    private async Task TryCleanUpAsync(IPAddress address, int port)
    {
        try
        {
            // close client immediately when failed to connect within timeout
            await DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger?.Error(
                e,
                $"Failed to close connect to the server {address}:{port}" +
                $" after connection timed out {_connectionTimeout.TotalMilliseconds}ms.");
        }
    }

    private void InitClient()
    {
        var addressFamily = _ipv6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        _client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        if (_ipv6Enabled)
        {
            _client.DualMode = true;
        }

        _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _socketKeepAliveEnabled);
    }

    private SslStream CreateSecureStream(Uri uri)
    {
        return new SslStream(
            ReaderStream,
            true,
            (_, certificate, chain, errors) =>
            {
                if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                {
                    _logger?.Error(null, $"{GetType().Name}: Certificate not available.");
                    return false;
                }

                var trust = _encryptionManager.TrustManager.ValidateServerCertificate(
                    uri,
                    new X509Certificate2(certificate.Export(X509ContentType.Cert)),
                    chain,
                    errors);

                if (trust)
                {
                    _logger?.Debug("Trust is established, resuming connection.");
                }
                else
                {
                    _logger?.Error(null, "Trust not established, aborting communication.");
                }

                return trust;
            });
    }
}
