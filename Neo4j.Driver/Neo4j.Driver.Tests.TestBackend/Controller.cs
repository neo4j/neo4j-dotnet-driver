using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Neo4j.Driver.Tests.TestBackend.Transaction;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Controller
    {
        private IConnection Connection { get; }
        private ProtocolObjectManager ObjManager { get; set; } = new ProtocolObjectManager();
        private bool BreakProcessLoop { get; set; } = false;
        private RequestReader RequestReader { get; set; }
        private ResponseWriter ResponseWriter { get; set; }
        public TransactionManager TransactionManagager { get; set; } = new TransactionManager();

        public Controller(IConnection conn)
        {
            Trace.WriteLine("Controller initialising");
            Connection = conn;
            ProtocolObjectFactory.ObjManager = ObjManager;
        }

        public async Task ProcessStreamObjects()
		{
            BreakProcessLoop = false;

            while (!BreakProcessLoop  &&  await RequestReader.ParseNextRequest().ConfigureAwait(false))
            {
                var protocolObject = ProtocolObjectFactory.CreateObject(RequestReader.GetObjectType(), RequestReader.CurrentObjectData);
                protocolObject.ProtocolEvent += BreakLoopEvent;

                await protocolObject.Process(this).ConfigureAwait(false);
                await SendResponse(protocolObject).ConfigureAwait(false);
                Trace.Flush();                
            }

            BreakProcessLoop = false;   //Ensure that any process loops that this one is running within still continue.
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

                    RequestReader = new RequestReader(connectionReader);
                    ResponseWriter = new ResponseWriter(connectionWriter);

                    Trace.WriteLine("Starting to listen for requests");

                    try
                    {
                        await ProcessStreamObjects().ConfigureAwait(false);
                    }
                    catch (Neo4jException ex)
                    {
                        // Generate "driver" exception something happened within the driver
                        await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                    }
                    catch (NotSupportedException ex)
                    {
                        // Get this sometimes during protocol handshake, like when connectiong with bolt:// on server
                        // with TLS. Could be a dirty read in the driver or a write from TLS server that causes strange
                        // version received..
                        await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                    }
                    catch (IOException ex)
                    {
                        Trace.WriteLine($"Socket exception detected: {ex.Message}");    //Handled outside of the exception manager because there is no connection to reply on.
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

        private void BreakLoopEvent(object sender, EventArgs e)
		{
            BreakProcessLoop = true;
		}

        public async Task SendResponse(IProtocolObject protocolObject)
		{
            await ResponseWriter.WriteResponseAsync(protocolObject).ConfigureAwait(false);
        }

        public async Task SendResponse(string response)
		{
            await ResponseWriter.WriteResponseAsync(response);
		}
    }
}
