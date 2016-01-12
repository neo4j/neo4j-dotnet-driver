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
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.messaging;
using Sockets.Plugin;

namespace Neo4j.Driver
{
    public class SocketClient
    {
        private readonly Config _config;
        private readonly Uri _url;
        private IPacker _packer;

        public SocketClient(Uri url, Config config)
        {
            _url = url;
            _config = config;
        }

        private static BigEndianTargetBitConverter BitConverter => new BigEndianTargetBitConverter();
        private TcpSocketClient TcpSocketClient { get; set; }

        public async Task Start()
        {
            var tcpSocketClient = new TcpSocketClient();
            await tcpSocketClient.ConnectAsync(_url.Host, _url.Port).ConfigureAwait(false);

            TcpSocketClient = tcpSocketClient;
            var version = await DoHandshake().ConfigureAwait(false);

            if (version != 1)
                throw new NotSupportedException("The Neo4j Server doesn't support this client.");

            _packer = new PackStreamV1Packer(TcpSocketClient, BitConverter);

//            Send(new InitMessage("hello world!"));
        }

        public async Task Stop()
        {
            if (TcpSocketClient != null)
            {
                await TcpSocketClient.DisconnectAsync().ConfigureAwait(false);
                TcpSocketClient.Dispose();
            }
        }

        public void Send(InitMessage message)
        {
            _packer.Pack(message);
            _packer.Flush();
        }

        private async Task<int> DoHandshake()
        {
            int[] supportedVersion = {1, 0, 0, 0};

            var data = PackVersions(supportedVersion);
            //            Logger.Log($"Sending Handshake... {string.Join(",", data)}");
            await TcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await TcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);

            data = new byte[4];
            //            Logger.Log("Receiving Handshake Reponse...");
            await TcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);

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
    }

    public static class SocketExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}