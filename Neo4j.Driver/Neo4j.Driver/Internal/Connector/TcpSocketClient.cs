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

namespace Neo4j.Driver.Internal.Connector
{
    internal class TcpSocketClient : ITcpSocketClient
    {
        private readonly TcpClient _client;
        private bool _useTls;
        private Stream _stream;
        private Uri _uri;

        private readonly EncryptionManager _encryptionManager;

        public TcpSocketClient(EncryptionManager encryptionManager)
        {
            _client = new TcpClient();
            _encryptionManager = encryptionManager;
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
                    var sslTask = secureStream.AuthenticateAsClientAsync(_uri.Host, null, System.Security.Authentication.SslProtocols.Tls12, false);
                    sslTask.Wait();
                    _stream = secureStream;
                }

                return _stream;
            }
        }

        private bool ServerValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return _encryptionManager.TrustStrategy.ValidateServerCertificate(_uri, certificate, sslPolicyErrors);
        }

        public Stream ReadStream => Stream;
        public Stream WriteStream => Stream;

        public async Task DisconnectAsync()
        {
            _client?.Dispose();
        }

        public async Task ConnectAsync(Uri uri, bool useTlsEncryption)
        {
            _uri = uri;
            _useTls = useTlsEncryption;
            await _client.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(true);
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