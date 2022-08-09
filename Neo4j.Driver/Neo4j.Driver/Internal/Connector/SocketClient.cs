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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Extensions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Util;

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

        private readonly ILogger _logger;

        public SocketClient(Uri uri, SocketSettings socketSettings, BufferSettings bufferSettings, ILogger logger = null,
            ITcpSocketClient socketClient = null)
        {
            _uri = uri;
            _logger = logger;
            _bufferSettings = bufferSettings;

            _tcpSocketClient = socketClient ?? new TcpSocketClient(socketSettings, _logger);
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

        public async Task<IBoltProtocol> ConnectAsync(IDictionary<string, string> routingContext, CancellationToken cancellationToken = default)
        {
            await _tcpSocketClient.ConnectAsync(_uri, cancellationToken).ConfigureAwait(false);

            SetOpened();
            _logger?.Debug($"~~ [CONNECT] {_uri}");
            var version = 
                await DoHandshakeAsync(cancellationToken)
                .ConfigureAwait(false);

            return SelectBoltProtocol(version, routingContext);
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

            return Task.CompletedTask;
        }

        private async Task<BoltProtocolVersion> DoHandshakeAsync(CancellationToken cancellationToken = default)
        {
            var data = BoltProtocolFactory.PackSupportedVersions();
            await _tcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            await _tcpSocketClient.WriteStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _logger?.Debug("C: [HANDSHAKE] {0}", data.ToHexString());

            data = new byte[4];
            var read = await _tcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            if (read < data.Length)
            {
                throw new IOException($"Unexpected end of stream when performing handshake, read returned {read}");
            }

            var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(data);
            _logger?.Debug("S: [HANDSHAKE] {0}.{1}", agreedVersion.MajorVersion, agreedVersion.MinorVersion);
            return agreedVersion;
        }

        private IBoltProtocol SelectBoltProtocol(BoltProtocolVersion version, IDictionary<string, string> routingContext)
        {
            var boltProtocol = BoltProtocolFactory.ForVersion(version, routingContext);
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

        public void SetRecvTimeOut(int seconds)
        {
            Reader.ReadTimeoutSeconds = seconds;
		}

        public void SetUseUtcEncodedDateTime(IBoltProtocol protocol)
        {
            Reader = protocol.NewReader(_tcpSocketClient.ReadStream, _bufferSettings, _logger, true);
            Writer = protocol.NewWriter(_tcpSocketClient.WriteStream, _bufferSettings, _logger, true);
        }
    }
}
