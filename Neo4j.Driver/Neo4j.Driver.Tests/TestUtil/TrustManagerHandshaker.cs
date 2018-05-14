// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using AuthenticationException = System.Security.Authentication.AuthenticationException;

namespace Neo4j.Driver.Tests.Connector.Trust
{
    internal class TrustManagerHandshaker
    {
        private IPEndPoint _listeningEndPoint = null;
        private readonly CountdownEvent _listeningEvent = new CountdownEvent(1);
        private readonly Uri _uri;
        private readonly X509Certificate2 _certificate;
        private readonly TrustManager _trustManager;

        public TrustManagerHandshaker(Uri uri, X509Certificate2 certificate, TrustManager trustManager)
        {
            _uri = uri;
            _certificate = certificate;
            _trustManager = trustManager;
        }

        public bool Perform()
        {
            var serverTask = Task.Run(() => StartServer());

            _listeningEvent.Wait();

            var result = false;
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect(_listeningEndPoint);

                var clientStream = new NetworkStream(client);
                var sslStream = new SslStream(clientStream, false, (sender, x509Certificate, chain, errors) =>
                    {
                        result = _trustManager.ValidateServerCertificate(_uri, (X509Certificate2)x509Certificate, chain, errors);
                        return result;
                    });

                try
                {
                    sslStream.AuthenticateAsClientAsync(_uri.Host, null, SslProtocols.Tls12, false).GetAwaiter()
                        .GetResult();
                }
                catch
                {
                    result = false;
                }
            }

            serverTask.Wait();

            return result;
        }

        private void StartServer()
        {
            using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                serverSocket.Listen(5);

                _listeningEndPoint = (IPEndPoint)serverSocket.LocalEndPoint;
                _listeningEvent.Signal();

                var accepted = serverSocket.Accept();
                var acceptedStream = new NetworkStream(accepted);
                var sslStream = new SslStream(acceptedStream, false);

                sslStream.AuthenticateAsServerAsync(_certificate, false, SslProtocols.Tls12, false).GetAwaiter()
                    .GetResult();
                sslStream.Dispose();
            }
        }

    }
}