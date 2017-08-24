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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using static System.Security.Authentication.SslProtocols;

namespace Neo4j.Driver.Internal.Connector
{
    internal class TcpSocketClient : ITcpSocketClient
    {
        private TcpClient _client;
        private Stream _stream;

        private readonly bool _ipv6Enabled;
        private readonly EncryptionManager _encryptionManager;
        private readonly TimeSpan _connectionTimeout;
        private readonly bool _socketKeepAliveEnabled;
        private readonly ILogger _logger;

        public Stream ReadStream => _stream;
        public Stream WriteStream => _stream;

        // only used in tests
        internal TcpClient Client => _client;

        public TcpSocketClient(SocketSettings socketSettings, ILogger logger = null)
        {
            _logger = logger;
            _encryptionManager = socketSettings.EncryptionManager;
            _ipv6Enabled = socketSettings.Ipv6Enabled;
            _connectionTimeout = socketSettings.ConnectionTimeout;
            _socketKeepAliveEnabled = socketSettings.SocketKeepAliveEnabled;
        }


        public async Task ConnectAsync(Uri uri)
        {
            await ConnectSocketAsync(uri).ConfigureAwait(false);
            if (!_encryptionManager.UseTls)
            {
                _stream = _client.GetStream();
            }
            else
            {
                try
                {
                    _stream = new SslStream(_client.GetStream(), true,
                        (sender, certificate, chain, errors) =>
                            _encryptionManager.TrustStrategy.ValidateServerCertificate(uri, certificate, errors));

                    await ((SslStream)_stream)
                        .AuthenticateAsClientAsync(uri.Host, null, Tls12, false).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new SecurityException($"Failed to establish encrypted connection with server {uri}.", e);
                }
            }
        }

        private async Task ConnectSocketAsync(Uri uri)
        {
            var addresses = await uri.ResolveAsync(_ipv6Enabled).ConfigureAwait(false);
            var innerErrors = new List<Exception>();
            for (var i = 0; i < addresses.Length; i++)
            {
                try
                {
                    await ConnectSocketAsync(addresses[i], uri.Port).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    var error = new IOException(
                        $"Failed to connect to server '{uri}' via IP address '{addresses[i]}': {e.Message}", e);
                    innerErrors.Add(error);

                    if (i == addresses.Length - 1)
                    {
                        // if all failed
                        throw new IOException(
                            $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.",
                            new AggregateException(innerErrors));
                    }
                }
            }
        }

        internal async Task ConnectSocketAsync(IPAddress address, int port)
        {
            var cancellationCompletionSource = new TaskCompletionSource<bool>();
            using (var cts = new CancellationTokenSource(_connectionTimeout))
            {
                InitClient();
                var connectTask = _client.ConnectAsync(address, port);
                using (cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true)))
                {
                    var finishedTask = await Task.WhenAny(connectTask, cancellationCompletionSource.Task).ConfigureAwait(false);
                    if (connectTask != finishedTask) // timed out
                    {
                        try
                        {
                            // close client immediately when failed to connect within timeout
                            Close();
                        }
                        catch (Exception e)
                        {
                            _logger?.Error($"Failed to close connect to the server {address}:{port}" +
                                           $" after connection timed out {_connectionTimeout.TotalMilliseconds}ms" +
                                           $" due to error: {e.Message}.", e);
                        }
                        throw new OperationCanceledException(
                            $"Failed to connect to server {address}:{port} within {_connectionTimeout.TotalMilliseconds}ms.", cts.Token);
                    }

                    await connectTask;
                }
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Close();
        }

        protected void Close()
        {
            _stream?.Dispose();
            CloseClient();

        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void InitClient()
        {
            if (_ipv6Enabled)
            {
                _client = new TcpClient(AddressFamily.InterNetworkV6) {Client = {DualMode = true}};
            }
            else
            {
                _client = new TcpClient();
            }
            _client.NoDelay = true;
            _client.Client.NoDelay = true;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive,
                _socketKeepAliveEnabled);
        }

        protected virtual void CloseClient()
        {
#if NET452
            _client?.Close();
#else
            _client?.Dispose();
#endif
        }
    }
}
