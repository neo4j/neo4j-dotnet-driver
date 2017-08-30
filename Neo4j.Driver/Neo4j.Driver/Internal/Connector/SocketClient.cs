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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
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
        private IBoltReader _reader;
        private IBoltWriter _writer;
        private readonly BufferSettings _bufferSettings;

        private readonly ILogger _logger;

        public SocketClient(Uri uri, SocketSettings socketSettings, BufferSettings bufferSettings, ILogger logger, ITcpSocketClient socketClient = null)
        {
            _uri = uri;
            _logger = logger;
            _bufferSettings = bufferSettings;
            _tcpSocketClient = socketClient ?? new TcpSocketClient(socketSettings, _logger);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            _tcpSocketClient.Connect(_uri);

            IsOpen = true;
            _logger?.Debug($"~~ [CONNECT] {_uri}");

            var version = DoHandshake();
            
            ConfigureVersion(version);
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
                            IsOpen = true;
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
                            ConfigureVersion(version);

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

        private void SetupPackStreamFormatWriterAndReader(bool supportBytes = true)
        {
            _writer = new BoltWriter(_tcpSocketClient.WriteStream, _bufferSettings.DefaultWriteBufferSize, _bufferSettings.MaxWriteBufferSize, _logger, supportBytes); 
            _reader = new BoltReader(_tcpSocketClient.ReadStream, _bufferSettings.DefaultReadBufferSize, _bufferSettings.MaxReadBufferSize, _logger, supportBytes);
        }

        private void Stop()
        {
            if (IsOpen)
            {
                _tcpSocketClient?.Dispose();
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
                Stop();
                throw;
            }
        }

        public Task SendAsync(IEnumerable<IRequestMessage> messages)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            try
            {
                foreach (var message in messages)
                {
                    _writer.Write(message);
                    _logger?.Debug("C: ", message);
                }

                _writer.FlushAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", t.Exception);
                        Stop();
                        taskCompletionSource.SetException(t.Exception.GetBaseException());
                    }
                    else if (t.IsCanceled)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    else
                    {
                        taskCompletionSource.SetResult(null);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception ex)
            {
                _logger?.Info($"Unable to send message to server {_uri}, connection will be terminated. ", ex);
                Stop();
                throw;
            }

            return taskCompletionSource.Task;
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
                await ReceiveOneAsync(responseHandler).ConfigureAwait(false);
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

        public Task ReceiveOneAsync(IMessageResponseHandler responseHandler)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            _reader.ReadAsync(responseHandler).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger?.Info($"Unable to read message from server {_uri}, connection will be terminated.", t.Exception);
                    Stop();

                    tcs.SetException(t.Exception.GetBaseException());
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    if (responseHandler.HasProtocolViolationError)
                    {
                        _logger?.Info(
                            $"Received bolt protocol error from server {_uri}, connection will be terminated.",
                            responseHandler.Error);
                        Stop();
                        tcs.SetException(responseHandler.Error);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
            });

            return tcs.Task;
        }

        private int DoHandshake()
        {
            int[] supportedVersion = {1, 0, 0, 0};
            
            var data = PackVersions(supportedVersion);
            _tcpSocketClient.WriteStream.Write(data, 0, data.Length);
            _tcpSocketClient.WriteStream.Flush();
            _logger?.Debug("C: [HANDSHAKE] [0x6060B017, 1, 0, 0, 0]");

            data = new byte[4];
            _tcpSocketClient.ReadStream.Read(data, 0, data.Length);

            var agreedVersion = GetAgreedVersion(data);
            _logger?.Debug($"S: [HANDSHAKE] {agreedVersion}");
            return agreedVersion;
        }

        private async Task<int> DoHandshakeAsync()
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

        private void ConfigureVersion(int version)
        {
            switch (version)
            {
                case ProtocolVersion.Version1:
                    SetupPackStreamFormatWriterAndReader();
                    break;
                case ProtocolVersion.NoVersion:
                    throw new NotSupportedException(
                        "The Neo4j server does not support any of the protocol versions supported by this client. " +
                        "Ensure that you are using driver and server versions that are compatible with one another.");
                case ProtocolVersion.Http:
                    throw new NotSupportedException(
                        "Server responded HTTP. Make sure you are not trying to connect to the http endpoint " +
                        $"(HTTP defaults to port 7474 whereas BOLT defaults to port {GraphDatabase.DefaultBoltPort})");
                default:
                    throw new NotSupportedException(
                        "Protocol error, server suggested unexpected protocol version: " + version);
            }
        }

        private static byte[] PackVersions(IEnumerable<int> versions)
        {
            //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
            var aLittleBitOfMagic = PackStreamBitConverter.GetBytes(0x6060B017);

            var bytes = new List<byte>(aLittleBitOfMagic);
            foreach (var version in versions)
            {
                bytes.AddRange(PackStreamBitConverter.GetBytes(version));
            }
            return bytes.ToArray();
        }


        private static int GetAgreedVersion(byte[] data)
        {
            return PackStreamBitConverter.ToInt32(data);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Stop();
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
