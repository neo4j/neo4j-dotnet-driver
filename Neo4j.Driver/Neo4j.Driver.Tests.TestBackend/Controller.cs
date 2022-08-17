using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Controller
    {
        private readonly bool _reactive;
        private IConnection Connection { get; }
        private ProtocolObjectManager ObjManager { get; set; } = new ProtocolObjectManager();
        private RequestReader RequestReader { get; set; }
        private ResponseWriter ResponseWriter { get; set; }
        public TransactionManager TransactionManager { get; set; } = new TransactionManager();
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public Controller(IConnection conn, bool reactive)
        {
            Connection = conn;
            _reactive = reactive;
            Trace.WriteLine("Controller initialising");
            ProtocolObjectFactory.ObjManager = ObjManager;
        }

        public async Task ProcessStreamObjects()
        {
            while (!CancellationTokenSource.IsCancellationRequested 
                   && await RequestReader.ParseNextRequest().ConfigureAwait(false))
            {
                var protocolObject = RequestReader.CreateObjectFromData();
                protocolObject.ProtocolEvent = CancellationTokenSource.Cancel;

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

                    await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                }
                catch (Exception ex)
                {
                    restartConnection = true;
                    storedException = ex;

                    Trace.WriteLine(TraceExceptionMessage(ex));

                    if (ex is TestKitProtocolException)
                        await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                }
                finally
                {
                    if (restartConnection)
                    {
                        Trace.WriteLine("Closing Connection");
                        Connection.Close();
                    }
                }
                Trace.Flush();
            }
        }

        public async Task<T> TryConsumeStreamObjectAsync<T>() where T : ProtocolObject
        {
            await RequestReader.ParseNextRequest().ConfigureAwait(false);

            if (RequestReader.GetObjectType() != typeof(T))
                return null;

            return (T)ProtocolObjectFactory.CreateObject(RequestReader.CurrentObjectData);
        }

        private async Task InitialiseCommunicationLayerAsync()
        {
            await Connection.Open();

            var connectionReader = new StreamReader(Connection.ConnectionStream, new UTF8Encoding(false));
            var connectionWriter = new StreamWriter(Connection.ConnectionStream, new UTF8Encoding(false));
            connectionWriter.NewLine = "\n";

            Trace.WriteLine("Connection open");

            RequestReader = new RequestReader(connectionReader);
            ResponseWriter = new ResponseWriter(connectionWriter);

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

        public Task SendResponseAsync(ProtocolObject protocolObject) => ResponseWriter.WriteResponseAsync(protocolObject);

        public Task SendResponseAsync(string response) => ResponseWriter.WriteResponseAsync(response);
        
    }
}
