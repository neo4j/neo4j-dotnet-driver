using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;

namespace Neo4j.Driver.Tests.TestBackend
{  
    internal class Controller
    {
        private IConnection Connection { get; }
        private Reader ConnectionReader { get; set; }
        private Writer ConnectionWriter { get; set; }
        private ProtocolObjectManager ObjManager { get; set; } = new ProtocolObjectManager();          

        public Controller(IConnection conn)
        {
            Trace.WriteLine("Controller initialising");
            Connection = conn;
            ProtocolObjectFactory.ObjManager = ObjManager;
        }

        public async Task Process()
        {
            try
            {
                Trace.WriteLine("Starting Controller.Process");

                while (true)
                {
                    await Connection.Open();

                    ConnectionReader = new Reader(Connection.ConnectionStream);
                    ConnectionWriter = new Writer(Connection.ConnectionStream);

                    Trace.WriteLine("Connection open");

                    RequestReader requestReader = new RequestReader(ConnectionReader);
                    ResponseWriter responseWriter = new ResponseWriter(ConnectionWriter);

                    Trace.WriteLine("Starting to listen for requests");

                    try
                    {
                        IProtocolObject protocolObject = null;
                        while ((protocolObject = await requestReader.ParseNextRequest().ConfigureAwait(false)) != null)
                        {
                            await protocolObject.Process().ConfigureAwait(false);

                            await responseWriter.WriteResponseAsync(protocolObject).ConfigureAwait(false);
                        }
                    }
                    catch(Exception ex)
                    {
                        Trace.WriteLine($"Exception thrown {ex.Message}\n{ex.StackTrace}");
                        
                        await responseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                    }                    
                    finally
                    {
                        Trace.WriteLine("Closing Connection");
                        Connection.Close();
                    }
                }                
            }
            catch(SocketException ex)
            {
                Trace.WriteLine($"Socket exception detected: {ex.Message}");
            }
            catch(Exception ex)
            {
                Trace.WriteLine($"It looks like the ExceptionExtensions system has failed in an unexpected way. \n{ex}");
            }
            finally
            {
                Connection.StopServer();
            }
        }
    }
}
