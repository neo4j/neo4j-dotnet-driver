using System;

namespace Neo4j.Driver
{
    public class Driver :IDisposable
    {
        private readonly Config _config;
        private readonly Uri _url;


        internal Driver(Uri url, Config config)
        {
            _url = url;
            _config = config;
        }

        public Session Session()
        {
            return new InternalSession(_url, _config);
        }

        public void Dispose()
        {
            // TODO close all the connections
            //throw new NotImplementedException();
        }
    }
}