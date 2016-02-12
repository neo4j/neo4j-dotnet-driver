//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Packstream;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;

namespace Neo4j.Driver.Internal.Connector
{
    public class SocketClient :  ISocketClient, IDisposable
    {
        private readonly Config _config;
        private readonly ITcpSocketClient _tcpSocketClient;
        private readonly Uri _url;
        private IReader _reader;
        private IWriter _writer;

        public SocketClient(Uri url, Config config, ITcpSocketClient socketClient = null)
        {
            _url = url;
            _config = config;
            _tcpSocketClient = socketClient ?? new TcpSocketClient();
        }

        private static BigEndianTargetBitConverter BitConverter => new BigEndianTargetBitConverter();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task Start()
        {
            await _tcpSocketClient.ConnectAsync(_url.Host, _url.Port, _config.TlsEnabled).ConfigureAwait(false);
            IsOpen = true;
            _config.Logger?.Debug($"~~ [CONNECT] {_url}");

            var version = await DoHandshake().ConfigureAwait(false);

            if (version != 1)
            {
                throw new NotSupportedException("The Neo4j Server doesn't support this client.");
            }
           _config.Logger?.Debug("S: [HANDSHAKE] 1");

            var formatV1 = new PackStreamMessageFormatV1(_tcpSocketClient, BitConverter, _config.Logger);
            _writer = formatV1.Writer;
            _reader = formatV1.Reader;
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

        public void Send(IEnumerable<IRequestMessage> messages, IMessageResponseHandler responseHandler)
        {
            foreach (var message in messages)
            {
                _writer.Write(message);
                _config.Logger?.Debug("C: ", message);
            }

            _writer.Flush();

            Receive(responseHandler);
        }

        public bool IsOpen { get; private set; }

        private void Receive(IMessageResponseHandler responseHandler)
        {
            while (!responseHandler.QueueIsEmpty())
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
            //Read 1 message
            //Send to handler,
            //While messages read < messages handled keep doing above.
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