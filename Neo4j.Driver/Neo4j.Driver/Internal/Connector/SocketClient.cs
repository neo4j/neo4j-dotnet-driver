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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Packstream;
using Neo4j.Driver.V1;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketClient :  ISocketClient, IDisposable
    {
        private readonly Config _config;
        private readonly ITcpSocketClient _tcpSocketClient;
        private readonly Uri _url;
        private IReader _reader;
        private IWriter _writer;

        private static class ProtocolVersion
        {
            public const int NoVersion = 0;
            public const int Version1 = 1;
            public const int Http = 1213486160;
        }

        public static readonly BigEndianTargetBitConverter BitConverter = new BigEndianTargetBitConverter();

        public SocketClient(Uri url, Config config, ITcpSocketClient socketClient = null)
        {
            _url = url;
            _config = config;
            _tcpSocketClient = socketClient ?? new TcpSocketClient();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task Start()
        {
            await _tcpSocketClient.ConnectAsync(_url.Host, _url.Port, _config.EncryptionLevel == EncryptionLevel.Encrypted).ConfigureAwait(false);
            IsOpen = true;
            _config.Logger?.Debug($"~~ [CONNECT] {_url}");

            var version = await DoHandshake().ConfigureAwait(false);

            switch (version)
            {
                case ProtocolVersion.Version1:
                    _config.Logger?.Debug("S: [HANDSHAKE] 1");

                    var formatV1 = new PackStreamMessageFormatV1(_tcpSocketClient, _config.Logger);
                    _writer = formatV1.Writer;
                    _reader = formatV1.Reader;
                    break;
                case ProtocolVersion.NoVersion:
                    throw new NotSupportedException("The Neo4j server does not support any of the protocol versions supported by this client. " +
                                                    "Ensure that you are using driver and server versions that are compatible with one another.");
                case ProtocolVersion.Http:
                    throw new NotSupportedException("Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                                                    "(HTTP defaults to port 7474 whereas BOLT defaults to port 7687)");
                default:
                    throw new NotSupportedException("Protocol error, server suggested unexpected protocol version: " + version);

            }
        }

        public async Task Stop()
        {
            if (IsOpen && _tcpSocketClient != null)
            {
                await _tcpSocketClient.DisconnectAsync().ConfigureAwait(false);
                _tcpSocketClient.Dispose();
            }
            IsOpen = false;
        }

        public void Send(IEnumerable<IRequestMessage> messages)
        {
            foreach (var message in messages)
            {
                _writer.Write(message);
                _config.Logger?.Debug("C: ", message);
            }
            _writer.Flush();
        }

        public bool IsOpen { get; private set; }

        /// <summary>
        /// This method highly relies on the fact that the session is not threadsafe and could only be used in a single thread
        /// as if two threads trying to modify the message size, then we might
        /// 1. force to pull all instead of streaming records
        /// 2. lose some records as only one record is buffered in result builder on client.
        /// </summary>
        public void Receive(IMessageResponseHandler responseHandler, int unhandledMessageSize = 0)
        {
            while (responseHandler.UnhandledMessageSize > unhandledMessageSize 
                || (responseHandler.HasError && responseHandler.UnhandledMessageSize > 0)
                /*if error happens, then just drain the whole unhandledMessage queue*/)
            {
                ReceiveOne(responseHandler);
                //Read 1 message
                //Send to handler
            }
        }

        /// <summary>
        /// This method will not throw exception if a railure message is received
        /// </summary>
        private void ReceiveOne(IMessageResponseHandler responseHandler)
        {
            try
            {
                _reader.Read(responseHandler);
            }
            catch (Exception ex)
            {
                _config.Logger.Error("Unable to unpack message from server, connection has been terminated.", ex);
                Task.Run(() => Stop()).Wait();
                throw;
            }
            if (responseHandler.HasError)
            {
                if (responseHandler.Error.Code.ToLowerInvariant().Contains("clienterror.request"))
                {
                    Task.Run(() => Stop()).Wait();
                    throw responseHandler.Error;
                }
            }
        }

        /// <summary>
        /// Return true if a record message is received, otherwise false.
        /// This method will throw the exception if a failure message is received.
        /// </summary>
        public bool ReceiveOneRecordMessage(IMessageResponseHandler responseHandler)
        {
            if (responseHandler.UnhandledMessageSize == 0)
            {
                return false;
            }
            ReceiveOne(responseHandler);
            if (responseHandler.HasError)
            {
                throw responseHandler.Error;
            }
            return responseHandler.IsRecordMessageReceived;
        }

        private async Task<int> DoHandshake()
        {
            _config.Logger?.Debug("C: [HANDSHAKE] [0x6060B017, 1, 0, 0, 0]");
            int[] supportedVersion = {1, 0, 0, 0};
            
            var data = PackVersions(supportedVersion);
            //            Logger.Log($"Sending Handshake... {string.Join(",", data)}");
            await _tcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await _tcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);

            data = new byte[4];
            //            Logger.Log("Receiving Handshake Reponse...");
            await _tcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);

            //            Logger.Log($"Handshake Raw = {string.Join(",", data)}");

            var agreedVersion = GetAgreedVersion(data);
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
    }
}