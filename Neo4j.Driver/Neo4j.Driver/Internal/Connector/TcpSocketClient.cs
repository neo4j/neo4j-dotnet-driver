// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class TcpSocketClient : ITcpSocketClient
    {
        private readonly TcpClient _client;
        private bool _useTls;
        private Stream _stream;
        private int _port;
        private string _host;
        private readonly ILogger _logger;

        public TcpSocketClient(ILogger logger)
        {
            _client = new TcpClient();
            _logger = logger;
        }
        
        private Stream Stream
        {
            get
            {
                if (_client == null || _client.Connected == false)
                {
                    throw new InvalidOperationException("Can't get stream if not connected.");
                }

                if(_stream != null) 
                    return _stream;

                if (!_useTls)
                {
                    _stream = _client.GetStream();
                }
                else
                {
                    var secureStream = new SslStream(_client.GetStream(), true, ServerValidationCallback);
                    var sslTask = secureStream.AuthenticateAsClientAsync(_host, null, System.Security.Authentication.SslProtocols.Tls12, false);
                    sslTask.Wait();
                    _stream = secureStream;
                }

                return _stream;
            }
        }

        private bool ServerValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    _logger.Debug("Server name mismatch.");
                    return false;
                case SslPolicyErrors.RemoteCertificateNotAvailable:
                    _logger.Debug("Certificate not available.");
                    return false;
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    _logger.Debug("Certificate validation failed.");
                    return false;
            }

            _logger.Debug("Authentication succeeded.");
            return true;
        }

        public Stream ReadStream => Stream;
        public Stream WriteStream => Stream;

        public async Task DisconnectAsync()
        {
            _client?.Dispose();
        }

        public async Task ConnectAsync(string host, int port, bool useTlsEncryption)
        {
            _host = host;
            _port = port;
            _useTls = useTlsEncryption;
            await _client.ConnectAsync(host, port).ConfigureAwait(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

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