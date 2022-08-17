using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class Controller
{
    private readonly bool _reactive;
    private readonly IConnection _connection;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private RequestReader _requestReader;
    private ResponseWriter _responseWriter;

    public TransactionManager TransactionManager { get; }

    public Controller(IConnection conn, bool reactive)
    {
        _connection = conn;
        _reactive = reactive;

        TransactionManager = new TransactionManager();
        _cancellationTokenSource = new CancellationTokenSource();

        Trace.WriteLine("Controller initializing");
    }

    public async Task ProcessStreamObjects()
    {
        while (!_cancellationTokenSource.IsCancellationRequested 
               && await _requestReader.ParseNextRequest().ConfigureAwait(false))
        {
            var protocolObject = _requestReader.CreateObjectFromData();
            protocolObject.ProtocolEvent = _cancellationTokenSource.Cancel;

            if (_reactive)
                await protocolObject.ReactiveProcessAsync(this).ConfigureAwait(false);
            else
                await protocolObject.ProcessAsync(this).ConfigureAwait(false);

            await SendResponseAsync(protocolObject).ConfigureAwait(false);
            Trace.Flush();
        }
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
                await ProcessStreamObjects().ConfigureAwait(false);
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
        await _requestReader.ParseNextRequest().ConfigureAwait(false);

        if (_requestReader.GetObjectType() != typeof(T))
            return null;

        return (T)ProtocolObjectFactory.CreateObject(_requestReader.CurrentObjectData);
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

    public Task SendResponseAsync(ProtocolObject protocolObject) => _responseWriter.WriteResponseAsync(protocolObject);

    public Task SendResponseAsync(string response) => _responseWriter.WriteResponseAsync(response);
        
}