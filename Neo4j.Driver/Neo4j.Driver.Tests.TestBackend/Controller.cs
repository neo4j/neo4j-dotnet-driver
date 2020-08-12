using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Neo4j.Driver.Tests.TestBackend
{  
    internal class Controller
    {
        private IConnection Connection { get; }
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

                    var connectionReader = new StreamReader(Connection.ConnectionStream, new UTF8Encoding(false));
                    var connectionWriter = new StreamWriter(Connection.ConnectionStream, new UTF8Encoding(false));
                    connectionWriter.NewLine = "\n";

                    Trace.WriteLine("Connection open");

                    RequestReader requestReader = new RequestReader(connectionReader);
                    ResponseWriter responseWriter = new ResponseWriter(connectionWriter);

                    Trace.WriteLine("Starting to listen for requests");

                    try
                    {
                        IProtocolObject protocolObject = null;
                        while ((protocolObject = await requestReader.ParseNextRequest().ConfigureAwait(false)) != null)
                        {
                            await protocolObject.Process().ConfigureAwait(false);

                            await responseWriter.WriteResponseAsync(protocolObject).ConfigureAwait(false);
                            Trace.Flush();
                        }
                    }
                    catch (IOException ex)
                    {
                        Trace.WriteLine($"Socket exception detected: {ex.Message}");    //Handled outside of the exception manager because there is no connection to reply on.
                    }
                    catch (Exception ex)
                    {   
                        await responseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                    }                    
                    finally
                    {
                        Trace.WriteLine("Closing Connection");
                        Connection.Close();
                    }
                    Trace.Flush();
                }                
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
