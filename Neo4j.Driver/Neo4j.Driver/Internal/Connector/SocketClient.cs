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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Packstream;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketClient :  ISocketClient
    {
        private static class ProtocolVersion
        {
            public const int NoVersion = 0;
            public const int Version1 = 1;
            public const int Http = 1213486160;
        }

        private readonly ITcpSocketClient _tcpSocketClient;
        private readonly Uri _uri;
        private IReader _reader;
        private IWriter _writer;

        private readonly ILogger _logger;

        public static readonly BigEndianTargetBitConverter BitConverter = new BigEndianTargetBitConverter();

        public SocketClient(Uri uri, EncryptionManager encryptionManager, bool socketKeepAlive, bool ipv6Enabled, ILogger logger, ITcpSocketClient socketClient = null)
        {
            _uri = uri;
            _logger = logger;
            _tcpSocketClient = socketClient ?? new TcpSocketClient(encryptionManager, socketKeepAlive, ipv6Enabled, _logger);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task Start()
        {
            return Start(Timeout.InfiniteTimeSpan);
        }

        public async Task Start(TimeSpan timeOut)
        {
            await _tcpSocketClient.ConnectAsync(_uri, timeOut).ConfigureAwait(false);
            IsOpen = true;
            _logger?.Debug($"~~ [CONNECT] {_uri}");

            var version = await DoHandshake().ConfigureAwait(false);

            switch (version)
            {
                case ProtocolVersion.Version1:
                    SetupPackStreamFormatWriterAndReader();
                    break;
                case ProtocolVersion.NoVersion:
                    throw new NotSupportedException("The Neo4j server does not support any of the protocol versions supported by this client. " +
                                                    "Ensure that you are using driver and server versions that are compatible with one another.");
                case ProtocolVersion.Http:
                    throw new NotSupportedException("Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                                                    $"(HTTP defaults to port 7474 whereas BOLT defaults to port {GraphDatabase.DefaultBoltPort})");
                default:
                    throw new NotSupportedException("Protocol error, server suggested unexpected protocol version: " + version);

            }
        }

        private void SetupPackStreamFormatWriterAndReader(bool supportBytes = true)
        {
            var formatV1 = new PackStreamMessageFormatV1(_tcpSocketClient, _logger, supportBytes);
            _writer = formatV1.Writer;
            _reader = formatV1.Reader;
        }


        private async Task Stop()
        {
            if (IsOpen && _tcpSocketClient != null)
            {
                _tcpSocketClient.Disconnect();
                _tcpSocketClient.Dispose();
            }
            IsOpen = false;
        }

        public void Send(IEnumerable<IRequestMessage> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    _writer.Write(message);
                    _logger?.Debug("C: ", message);
                }
                _writer.Flush();
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", ex);
                Task.Run(() => Stop()).Wait();
                throw;
            }
        }

        public async Task SendAsync(IEnumerable<IRequestMessage> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    _writer.Write(message);
                    _logger?.Debug("C: ", message);
                }

                await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", ex);
                Task.Run(() => Stop()).Wait();
                throw;
            }
        }

        public bool IsOpen { get; private set; }

        public void Receive(IMessageResponseHandler responseHandler)
        {
            while(responseHandler.UnhandledMessageSize > 0)
            {
                ReceiveOne(responseHandler);
            }
        }

        public async Task ReceiveAsync(IMessageResponseHandler responseHandler)
        {
            while (responseHandler.UnhandledMessageSize > 0)
            {
                await ReceiveOneAsync(responseHandler);
            }
        }


        public void ReceiveOne(IMessageResponseHandler responseHandler)
        {
            try
            {
                _reader.Read(responseHandler);
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to read message from server {_uri}, connection will be terminated.", ex);
                Task.Run(() => Stop()).Wait();
                throw;
            }
            if (responseHandler.HasProtocolViolationError)
            {
                _logger?.Info($"Received bolt protocol error from server {_uri}, connection will be terminated.", responseHandler.Error);
                Task.Run(() => Stop()).Wait();
                throw responseHandler.Error;
            }
        }

        public async Task ReceiveOneAsync(IMessageResponseHandler responseHandler)
        {
            try
            {
                await _reader.ReadAsync(responseHandler);
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to read message from server {_uri}, connection will be terminated.", ex);
                Task.Run(() => Stop()).Wait();
                throw;
            }
            if (responseHandler.HasProtocolViolationError)
            {
                _logger?.Info($"Received bolt protocol error from server {_uri}, connection will be terminated.", responseHandler.Error);
                Task.Run(() => Stop()).Wait();
                throw responseHandler.Error;
            }
        }

        private async Task<int> DoHandshake()
        {
            int[] supportedVersion = {1, 0, 0, 0};
            
            var data = PackVersions(supportedVersion);
            await _tcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await _tcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);
            _logger?.Debug("C: [HANDSHAKE] [0x6060B017, 1, 0, 0, 0]");

            data = new byte[4];
            await _tcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);

            var agreedVersion = GetAgreedVersion(data);
            _logger?.Debug($"S: [HANDSHAKE] {agreedVersion}");
            return agreedVersion;
        }

        private static byte[] PackVersions(IEnumerable<int> versions)
        {
            //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
            var aLittleBitOfMagic = BitConverter.GetBytes(0x6060B017);

            var bytes = new List<byte>(aLittleBitOfMagic);
            foreach (var version in versions)
            {
                bytes.AddRange(BitConverter.GetBytes(version));
            }
            return bytes.ToArray();
        }


        private static int GetAgreedVersion(byte[] data)
        {
            return BitConverter.ToInt32(data);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Task.Run(() => Stop()).Wait();
        }

        public void UpdatePackStream(string serverVersion)
        {
            var version = ServerVersion.Version(serverVersion);
            if ( version >= ServerVersion.V3_2_0 )
            {
                return;
            }
            SetupPackStreamFormatWriterAndReader(false);
        }
    }
}