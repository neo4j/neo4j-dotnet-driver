// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketClient : ISocketClient
    {
        private const string MessagePattern = "C: {0}";
        private readonly Uri _uri;
        private readonly BufferSettings _bufferSettings;

        public IMessageReader Reader { get; private set; }
        public IMessageWriter Writer { get; private set; }
        private readonly ITcpSocketClient _tcpSocketClient;

        private int _closedMarker = -1;

        private readonly IDriverLogger _logger;
        private readonly IConnectionListener _connMetricsListener;
        private readonly IListenerEvent _connEvent;

        public SocketClient(Uri uri, SocketSettings socketSettings, BufferSettings bufferSettings,
            IConnectionListener connMetricsListener = null, IDriverLogger logger = null,
            ITcpSocketClient socketClient = null)
        {
            _uri = uri;
            _logger = logger;
            _bufferSettings = bufferSettings;
            _tcpSocketClient = socketClient ?? new TcpSocketClient(socketSettings, _logger);

            _connMetricsListener = connMetricsListener;
            if (_connMetricsListener != null)
            {
                _connEvent = new SimpleTimerEvent();
            }
        }

        // For testing only
        internal SocketClient(IMessageReader reader, IMessageWriter writer, ITcpSocketClient socketClient = null)
        {
            Reader = reader;
            Writer = writer;
            _tcpSocketClient = socketClient;
        }

        public bool IsOpen => _closedMarker == 0;
        private bool IsClosed => _closedMarker > 0;

        public async Task<IBoltProtocol> ConnectAsync()
        {
            _connMetricsListener?.ConnectionConnecting(_connEvent);
            await _tcpSocketClient.ConnectAsync(_uri).ConfigureAwait(false);

            SetOpened();
            _logger?.Debug($"~~ [CONNECT] {_uri}");
            _connMetricsListener?.ConnectionConnected(_connEvent);

            var version = await DoHandshakeAsync().ConfigureAwait(false);
            return SelectBoltProtocol(version);
        }

        public async Task SendAsync(IEnumerable<IRequestMessage> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    Writer.Write(message);
                    LogDebug(MessagePattern, message);
                }

                await Writer.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex, $"Unable to send message to server {_uri}, connection will be terminated.");
                await StopAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task ReceiveAsync(IResponsePipeline responsePipeline)
        {
            while (!responsePipeline.HasNoPendingMessages)
            {
                await ReceiveOneAsync(responsePipeline).ConfigureAwait(false);
            }
        }

        public async Task ReceiveOneAsync(IResponsePipeline responsePipeline)
        {
            try
            {
                await Reader.ReadAsync(responsePipeline).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"Unable to read message from server {_uri}, connection will be terminated.");
                await StopAsync().ConfigureAwait(false);
                throw;
            }

            // We force ProtocolException's to be thrown here to shortcut the communication with the server
            try
            {
                responsePipeline.AssertNoProtocolViolation();
            }
            catch (ProtocolException exc)
            {
                _logger?.Warn(exc, "A bolt protocol error has occurred with server {0}, connection will be terminated.",
                    _uri.ToString());
                await StopAsync().ConfigureAwait(false);
                throw;
            }
        }

        internal void SetOpened()
        {
            Interlocked.CompareExchange(ref _closedMarker, 0, -1);
        }

        public Task StopAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return _tcpSocketClient.DisconnectAsync();
            }

            return TaskHelper.GetCompletedTask();
        }

        private async Task<int> DoHandshakeAsync()
        {
            var data = BoltProtocolFactory.PackSupportedVersions();
            await _tcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await _tcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);
            _logger?.Debug("C: [HANDSHAKE] {0}", data.ToHexString());

            data = new byte[4];
            var read = await _tcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);
            if (read <= 0)
            {
                throw new IOException($"Unexpected end of stream, read returned {read}");
            }

            var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(data);
            _logger?.Debug("S: [HANDSHAKE] {0}", agreedVersion);
            return agreedVersion;
        }

        private IBoltProtocol SelectBoltProtocol(int version)
        {
            var boltProtocol = BoltProtocolFactory.ForVersion(version);
            Reader = boltProtocol.NewReader(_tcpSocketClient.ReadStream, _bufferSettings, _logger);
            Writer = boltProtocol.NewWriter(_tcpSocketClient.WriteStream, _bufferSettings, _logger);
            return boltProtocol;
        }

        private void LogDebug(string message, params object[] args)
        {
            if (_logger != null && _logger.IsDebugEnabled())
            {
                _logger?.Debug(message, args);
            }
        }
    }
}