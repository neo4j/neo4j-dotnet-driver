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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using static System.Security.Authentication.SslProtocols;

namespace Neo4j.Driver.Internal.Connector
{
    internal class TcpSocketClient : ITcpSocketClient
    {
        private Socket _client;
        private Stream _stream;

        private readonly IHostResolver _resolver;
        private readonly bool _ipv6Enabled;
        private readonly EncryptionManager _encryptionManager;
        private readonly TimeSpan _connectionTimeout;
        private readonly bool _socketKeepAliveEnabled;
        private readonly IDriverLogger _logger;

        public Stream ReadStream => _stream;
        public Stream WriteStream => _stream;

        public TcpSocketClient(SocketSettings socketSettings, IDriverLogger logger = null)
        {
            Throw.ArgumentNullException.IfNull(socketSettings, nameof(socketSettings));
            Throw.ArgumentNullException.IfNull(socketSettings.HostResolver, nameof(SocketSettings.HostResolver));
            Throw.ArgumentNullException.IfNull(socketSettings.EncryptionManager, nameof(SocketSettings.EncryptionManager));

            _logger = logger;
            _resolver = socketSettings.HostResolver;
            _encryptionManager = socketSettings.EncryptionManager;
            _ipv6Enabled = socketSettings.Ipv6Enabled;
            _connectionTimeout = socketSettings.ConnectionTimeout;
            _socketKeepAliveEnabled = socketSettings.SocketKeepAliveEnabled;
        }


        public void Connect(Uri uri)
        {
            ConnectSocket(uri);
            
            _stream = new NetworkStream(_client);
            if (_encryptionManager.UseTls)
            {
                try
                {
                    var secureStream = CreateSecureStream(uri);
                    
#if NET452
                    secureStream.AuthenticateAsClient(uri.Host, null, Tls12, false);
#else
                    secureStream.AuthenticateAsClientAsync(uri.Host, null, Tls12, false).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
#endif

                    _stream = secureStream;
                }
                catch (Exception e)
                {
                    throw new SecurityException($"Failed to establish encrypted connection with server {uri}.", e);
                }
            }
        }

        public async Task ConnectAsync(Uri uri)
        {
            await ConnectSocketAsync(uri).ConfigureAwait(false);

            _stream = new NetworkStream(_client);
            if (_encryptionManager.UseTls)
            {
                try
                {
                    _stream = CreateSecureStream(uri);

                    await ((SslStream)_stream)
                        .AuthenticateAsClientAsync(uri.Host, null, Tls12, false).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new SecurityException($"Failed to establish encrypted connection with server {uri}.", e);
                }
            }
        }

        private void ConnectSocket(Uri uri)
        {
            var innerErrors = new List<Exception>();
            var addresses = _resolver.Resolve(uri.Host);
                        
            foreach (var address in addresses)
            {
                try
                {
                    ConnectSocket(address, uri.Port);
                    
                    return;
                }
                catch (Exception e)
                {
                    var actualException = e;
                    if (actualException is AggregateException)
                    {
                        actualException = ((AggregateException) actualException).GetBaseException();
                    }

                    innerErrors.Add(new IOException(
                        $"Failed to connect to server '{uri}' via IP address '{address}': {actualException.Message}",
                        actualException));
                }
            }
            
            // all failed
            throw new IOException(
                $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.",
                new AggregateException(innerErrors));
        }

        private async Task ConnectSocketAsync(Uri uri)
        {
            var innerErrors = new List<Exception>();
            var addresses = await _resolver.ResolveAsync(uri.Host).ConfigureAwait(false);
                        
            foreach (var address in addresses)
            {
                try
                {
                    await ConnectSocketAsync(address, uri.Port).ConfigureAwait(false);
                    
                    return;
                }
                catch (Exception e)
                {
                    var actualException = e;
                    if (actualException is AggregateException)
                    {
                        actualException = ((AggregateException) actualException).GetBaseException();
                    }

                    innerErrors.Add(new IOException(
                        $"Failed to connect to server '{uri}' via IP address '{address}': {actualException.Message}",
                        actualException));
                }
            }
            
            // all failed
            throw new IOException(
                $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.",
                new AggregateException(innerErrors));
        }

        internal void ConnectSocket(IPAddress address, int port)
        {
            InitClient();

            using (var cts = new CancellationTokenSource(_connectionTimeout))
            {
#if NET452
                try
                {
                    _client.Connect(address, port);
                }
                catch (SocketException ex) when (ex.Message.Contains(
                    "An address incompatible with the requested protocol was used"))
                {
                    throw new NotSupportedException("This protocol version is not supported.");
                }
#else
                _client.ConnectAsync(address, port).ConfigureAwait(false).GetAwaiter().GetResult();
#endif

                if (cts.IsCancellationRequested)
                {
                    try
                    {
                        // close client immediately when failed to connect within timeout
                        Disconnect();
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, $"Failed to close connect to the server {address}:{port}" +
                                          $" after connection timed out {_connectionTimeout.TotalMilliseconds}ms.");
                    }

                    throw new OperationCanceledException(
                        $"Failed to connect to server {address}:{port} within {_connectionTimeout.TotalMilliseconds}ms.",
                        cts.Token);
                }
            }
        }

        internal async Task ConnectSocketAsync(IPAddress address, int port)
        {
            InitClient();

#if NET452
            var connectTask = Task.Factory.FromAsync(_client.BeginConnect, _client.EndConnect, address, port, null);
#else
            var connectTask = _client.ConnectAsync(address, port);
#endif
            var finishedTask = await Task.WhenAny(connectTask, Task.Delay(_connectionTimeout)).ConfigureAwait(false);
            if (connectTask != finishedTask) // timed out
            {
                try
                {
                    // close client immediately when failed to connect within timeout
                    await DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, $"Failed to close connect to the server {address}:{port}" +
                                      $" after connection timed out {_connectionTimeout.TotalMilliseconds}ms.");
                }
                
                throw new OperationCanceledException(
                    $"Failed to connect to server {address}:{port} within {_connectionTimeout.TotalMilliseconds}ms.");
            }

            await connectTask.ConfigureAwait(false);
        }

        public virtual void Disconnect()
        {
            if (_client != null)
            {
                if (_client.Connected)
                {
                    _client.Shutdown(SocketShutdown.Both);
                }

                _client.Dispose();

                _client = null;
                _stream = null;
            }   
        }

        public virtual Task DisconnectAsync()
        {
            if (_client != null)
            {
                if (_client.Connected)
                {
#if NET452
                    return Task.Factory.FromAsync(_client.BeginDisconnect, _client.EndDisconnect, false, null)
                        .ContinueWith(
                            t =>
                            {
                                _client.Dispose();
                                _stream?.Dispose();

                                _client = null;
                                _stream = null;

                                return TaskHelper.GetCompletedTask();
                            }).Unwrap();
#else
                    _client.Shutdown(SocketShutdown.Both);
#endif
                }

                _client.Dispose();

                _client = null;
                _stream = null;
            }

            return TaskHelper.GetCompletedTask();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Disconnect();
        }

        private void InitClient()
        {
            if (_ipv6Enabled)
            {
                _client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
                {
                    DualMode = true
                };
            }
            else
            {
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            _client.NoDelay = true;
            _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _socketKeepAliveEnabled);
        }

        private SslStream CreateSecureStream(Uri uri)
        {
            return new SslStream(_stream, true,
                (sender, certificate, chain, errors) =>
                {
                    if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                    {
                        _logger?.Error(null, $"{GetType().Name}: Certificate not available.");
                        return false;
                    }

                    var trust = _encryptionManager.TrustManager.ValidateServerCertificate(uri,
                        new X509Certificate2(certificate.Export(X509ContentType.Cert)), chain, errors);

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
}
