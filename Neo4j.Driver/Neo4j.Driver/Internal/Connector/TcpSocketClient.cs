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
using System.IO;
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
        private readonly TcpClient _client;
        private Stream _stream;
        private readonly bool _ipv6Enabled = false;

        private readonly EncryptionManager _encryptionManager;

        public TcpSocketClient(EncryptionManager encryptionManager, bool keepAlive, bool ipv6Enabled, ILogger logger = null)
        {
            _encryptionManager = encryptionManager;
            _ipv6Enabled = ipv6Enabled;
            if (_ipv6Enabled)
            {
                _client = new TcpClient(AddressFamily.InterNetworkV6);
                _client.Client.DualMode = true;
            }
            else
            {
                _client = new TcpClient();
            }
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, keepAlive);
        }

        public Stream ReadStream => _stream;
        public Stream WriteStream => _stream;

        public async Task ConnectAsync(Uri uri, TimeSpan timeOut)
        {
            await Connect(uri, timeOut);
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

        private async Task Connect(Uri uri, TimeSpan timeOut)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource(timeOut))
            {
                var addresses = await uri.ResolveAsync(_ipv6Enabled);
                AggregateException innerErrors = null;
                for (var i = 0; i < addresses.Length; i++)
                {
                    try
                    {
                        cancellationSource.Token.ThrowIfCancellationRequested();

                        await _client.ConnectAsync(addresses[i], uri.Port).ConfigureAwait(false);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        var error = new IOException($"Failed to connect to server '{uri}' via IP address '{addresses[i]}': {e.Message}", e);
                        innerErrors = innerErrors == null ? new AggregateException(error) : new AggregateException(innerErrors, error);

                        if (i == addresses.Length - 1)
                        {
                            // if all failed
                            throw new IOException(
                                $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.", innerErrors);
                        }
                    }
                }
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Close();
        }

        private void Close()
        {
            _stream?.Dispose();
            _client?.Dispose();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}