using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{  
    internal class Controller
    {
        private IConnection Connection { get; }
        private Reader ConnectionReader { get; set; }
        private Writer ConnectionWriter { get; set; }
        private ProtocolObjectFactory ProtocolFactory { get; set; }
        private ProtocolObjectManager ObjManager { get; set; } = new ProtocolObjectManager();       
        

        public Controller(IConnection conn)
        {
            Trace.WriteLine("Controller initialising");
            Connection = conn;            
            ProtocolFactory = new ProtocolObjectFactory(ObjManager);
        }

        public async Task Process()
        {
            Trace.WriteLine("Starting Controller.Process");

            await Connection.Open();

            ConnectionReader = new Reader(Connection.ConnectionStream);
            ConnectionWriter = new Writer(Connection.ConnectionStream);

            Trace.WriteLine("Connection open");

            RequestReader requestReader = new RequestReader(ConnectionReader, ProtocolFactory);
            ResponseWriter responseWriter = new ResponseWriter(ConnectionWriter);

            Trace.WriteLine("Starting to listen for requests");

            try
            {
                while (Connection.Connected)
                {
                    Trace.WriteLine("Listening for request");
                    var protocolObject = await requestReader.ParseNextRequest().ConfigureAwait(false);

                    await protocolObject.Process().ConfigureAwait(false);

                    await responseWriter.WriteResponseAsync(protocolObject).ConfigureAwait(false);
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine($"Exception thrown {ex.Message}\n{ex.StackTrace}");
            }
           
            Trace.WriteLine("Connection no longer active, Controller.Process ending");
        }
    }
}
