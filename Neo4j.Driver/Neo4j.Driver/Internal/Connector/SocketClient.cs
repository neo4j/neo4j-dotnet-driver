// Copyright (c) 2002-2018 "Neo Technology,"
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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketClient :  ISocketClient
    {
        private readonly Uri _uri;
        private readonly BufferSettings _bufferSettings;

        private readonly ITcpSocketClient _tcpSocketClient;
        private IBoltProtocol _boltProtocol;

        private int _closedMarker = -1;

        private readonly ILogger _logger;

        public SocketClient(Uri uri, SocketSettings socketSettings, BufferSettings bufferSettings, ILogger logger, ITcpSocketClient socketClient = null)
        {
            _uri = uri;
            _logger = logger;
            _bufferSettings = bufferSettings;
            _tcpSocketClient = socketClient ?? new TcpSocketClient(socketSettings, _logger);
        }

        // For testing only
        internal SocketClient(IBoltProtocol boltProtocol, ITcpSocketClient socketClient = null)
        {
            _boltProtocol = boltProtocol;
            _tcpSocketClient = socketClient;
        }

        public bool IsOpen => _closedMarker == 0;

        private bool IsClosed => _closedMarker > 0;

        public void Start()
        {
            _tcpSocketClient.Connect(_uri);

            SetOpened();
            _logger?.Debug($"~~ [CONNECT] {_uri}");

            var version = DoHandshake();
            _boltProtocol = BoltProtocolFactory.Create(version, _tcpSocketClient, _bufferSettings, _logger);
        }

        public Task StartAsync()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            _tcpSocketClient.ConnectAsync(_uri)
                .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            tcs.SetException(t.Exception.GetBaseException());
                        }
                        else if (t.IsCanceled)
                        {
                            tcs.SetCanceled();
                        }
                        else
                        {
                            SetOpened();
                            _logger?.Debug($"~~ [CONNECT] {_uri}");

                            return DoHandshakeAsync();
                        }

                        return Task.FromResult(-1);
                    }, TaskContinuationOptions.ExecuteSynchronously).Unwrap()
                .ContinueWith(t =>
                {
                    int version = t.Result;

                    if (version != -1)
                    {
                        try
                        {
                            _boltProtocol = BoltProtocolFactory.Create(version, _tcpSocketClient, _bufferSettings, _logger);
                            tcs.SetResult(null);
                        }
                        catch (AggregateException exc)
                        {
                            tcs.SetException(exc.GetBaseException());
                        }
                        catch (Exception exc)
                        {
                            tcs.SetException(exc);
                        }
                    }   
                }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public void Send(IEnumerable<IRequestMessage> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    _boltProtocol.Writer.Write(message);
                    _logger?.Debug("C: ", message);
                }
                _boltProtocol.Writer.Flush();
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", ex);
                Stop();
                throw;
            }
        }

        public async Task SendAsync(IEnumerable<IRequestMessage> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    _boltProtocol.Writer.Write(message);
                    _logger?.Debug("C: ", message);
                }
                await _boltProtocol.Writer.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", ex);
                await StopAsync().ConfigureAwait(false);
                throw;
            }
        }

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
                await ReceiveOneAsync(responseHandler).ConfigureAwait(false);
            }
        }

        public void ReceiveOne(IMessageResponseHandler responseHandler)
        {
            try
            {
                _boltProtocol.Reader.Read(responseHandler);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Unable to read message from server {_uri}, connection will be terminated.", ex);
                Stop();
                throw;
            }
            if (responseHandler.HasProtocolViolationError)
            {
                _logger?.Info($"Received bolt protocol error from server {_uri}, connection will be terminated.", responseHandler.Error);
                Stop();
                throw responseHandler.Error;
            }
        }

        public async Task ReceiveOneAsync(IMessageResponseHandler responseHandler)
        {
            try
            {
                await _boltProtocol.Reader.ReadAsync(responseHandler).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Unable to read message from server {_uri}, connection will be terminated.", ex);
                await StopAsync().ConfigureAwait(false);
                throw;
            }
            if (responseHandler.HasProtocolViolationError)
            {
                _logger?.Info($"Received bolt protocol error from server {_uri}, connection will be terminated.", responseHandler.Error);
                await StopAsync().ConfigureAwait(false);
                throw responseHandler.Error;
            }
       }

        public void UpdateBoltProtocol(string serverVersion)
        {
            _boltProtocol.ReconfigIfNecessary(serverVersion);
        }

        internal void SetOpened()
        {
            Interlocked.CompareExchange(ref _closedMarker, 0, -1);
        }


        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                _tcpSocketClient.Disconnect();
            }
        }

        public Task StopAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return _tcpSocketClient.DisconnectAsync();
            }

            return TaskExtensions.GetCompletedTask();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsClosed)
                return;

            if (disposing)
            {
                Stop();
            }
        }

        private int DoHandshake()
        {
            var data = BoltProtocolFactory.PackSupportedVersions();
            _tcpSocketClient.WriteStream.Write(data, 0, data.Length);
            _tcpSocketClient.WriteStream.Flush();
            _logger?.Debug("C: [HANDSHAKE] ", data);

            data = new byte[4];
            _tcpSocketClient.ReadStream.Read(data, 0, data.Length);

            var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(data);
            _logger?.Debug($"S: [HANDSHAKE] {agreedVersion}");
            return agreedVersion;
        }

        private async Task<int> DoHandshakeAsync()
        {
            var data = BoltProtocolFactory.PackSupportedVersions();
            await _tcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await _tcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);
            _logger?.Debug("C: [HANDSHAKE] ", data);

            data = new byte[4];
            await _tcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);

            var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(data);
            _logger?.Debug($"S: [HANDSHAKE] {agreedVersion}");
            return agreedVersion;
        }
    }
}
