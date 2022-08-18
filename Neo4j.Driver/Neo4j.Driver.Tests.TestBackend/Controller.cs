// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class Controller
{
    private bool BreakProcessLoop;
    private readonly IConnection _connection;
    private readonly bool _reactive;
    private RequestReader _requestReader;
    private ResponseWriter _responseWriter;

    public Controller(IConnection conn, bool reactive)
    {
        _connection = conn;
        _reactive = reactive;

        TransactionManager = new TransactionManager<IAsyncTransaction>();
        ReactiveTransactionManager = new TransactionManager<IRxTransaction>();

        Trace.WriteLine("Controller initializing");
    }

    public TransactionManager<IAsyncTransaction> TransactionManager { get; }
    public TransactionManager<IRxTransaction> ReactiveTransactionManager { get; set; }

    public async Task ProcessStreamObjects()
    {
        BreakProcessLoop = false;

        while (!BreakProcessLoop
               && await _requestReader.ParseNextRequest())
        {
            var protocolObject = _requestReader.CreateObjectFromData();
            protocolObject.ProtocolEvent = () => BreakProcessLoop = true;

            if (_reactive)
                await protocolObject.ReactiveProcessAsync(this);
            else
                await protocolObject.ProcessAsync(this);

            await SendResponseAsync(protocolObject);
            Trace.Flush();
        }

        BreakProcessLoop = false;
    }

    public async Task ProcessAsync(bool restartInitialState, Func<Exception, bool> loopConditional)
    {
        var restartConnection = restartInitialState;

        Trace.WriteLine("Starting Controller.Process");

        Exception storedException = new TestKitClientException("Error from client");

        while (loopConditional(storedException))
        {
            if (restartConnection)
                await InitialiseCommunicationLayerAsync();

            try
            {
                await ProcessStreamObjects();
            }
            catch (Exception ex) when (NoResetException(ex))
            {
                restartConnection = false;
                storedException = ex;

                await _responseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
            }
            catch (Exception ex)
            {
                restartConnection = true;
                storedException = ex;

                Trace.WriteLine(TraceExceptionMessage(ex));

                if (ex is TestKitProtocolException)
                    await _responseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
            }
            finally
            {
                if (restartConnection)
                {
                    Trace.WriteLine("Closing Connection");
                    _connection.Close();
                }
            }

            Trace.Flush();
        }
    }

    public async Task<T> TryConsumeStreamObjectAsync<T>() where T : ProtocolObject
    {
        await _requestReader.ParseNextRequest();

        if (_requestReader.GetObjectType() != typeof(T))
            return null;

        return (T) ProtocolObjectFactory.CreateObject(_requestReader.CurrentObjectData);
    }

    private async Task InitialiseCommunicationLayerAsync()
    {
        await _connection.Open();

        var connectionReader = new StreamReader(_connection.ConnectionStream, new UTF8Encoding(false));
        var connectionWriter = new StreamWriter(_connection.ConnectionStream, new UTF8Encoding(false));
        connectionWriter.NewLine = "\n";

        Trace.WriteLine("Connection open");

        _requestReader = new RequestReader(connectionReader);
        _responseWriter = new ResponseWriter(connectionWriter);

        Trace.WriteLine("Starting to listen for requests");
    }

    private static string TraceExceptionMessage(Exception ex)
    {
        return ex switch
        {
            IOException => $"Socket exception detected: {ex.Message}",
            TestKitProtocolException => $"TestKit protocol exception detected: {ex.Message}",
            _ => $"General exception detected, restarting connection: {ex.Message}"
        };
    }

    private bool NoResetException(Exception ex)
    {
        return ex is Neo4jException or TestKitClientException or ArgumentException
            or NotSupportedException or JsonSerializationException or DriverExceptionWrapper;
    }

    public Task SendResponseAsync(ProtocolObject protocolObject)
    {
        return _responseWriter.WriteResponseAsync(protocolObject);
    }

    public Task SendResponseAsync(string response)
    {
        return _responseWriter.WriteResponseAsync(response);
    }
}