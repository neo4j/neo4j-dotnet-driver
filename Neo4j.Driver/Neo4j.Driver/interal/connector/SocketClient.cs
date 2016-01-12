using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sockets.Plugin;

namespace Neo4j.Driver
{
    public class SocketClient
    {
        private readonly Config _config;
        private readonly Uri _url;
        private static BigEndianTargetBitConverter BitConverter => new BigEndianTargetBitConverter();
        private TcpSocketClient TcpSocketClient { get; set; }

        public SocketClient(Uri url, Config config)
        {
            _url = url;
            _config = config;
        }

        public void Dispose()
        {
            Stop();
        }

        public async Task Start()
        {
            var tcpSocketClient = new TcpSocketClient();
            await tcpSocketClient.ConnectAsync(_url.Host, _url.Port).ConfigureAwait(false);
          
            TcpSocketClient = tcpSocketClient;
            await DoHandshake().ConfigureAwait(false);
        }

        public async Task Stop()
        {
            if (TcpSocketClient != null)
            {
                await TcpSocketClient.DisconnectAsync().ConfigureAwait(false);
                TcpSocketClient.Dispose();
            }
        }
        private async Task<int> DoHandshake()
        {

            int[] supportedVersion = { 1, 0, 0, 0 };

            byte[] data = PackVersions(supportedVersion);
            //            Logger.Log($"Sending Handshake... {string.Join(",", data)}");
            await TcpSocketClient.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await TcpSocketClient.WriteStream.FlushAsync().ConfigureAwait(false);

            data = new byte[4];
            //            Logger.Log("Receiving Handshake Reponse...");
            await TcpSocketClient.ReadStream.ReadAsync(data, 0, data.Length).ConfigureAwait(false);

            //            Logger.Log($"Handshake Raw = {string.Join(",", data)}");

            var agreedVersion = GetAgreedVersion(data);
            return agreedVersion;
        }

        private static byte[] PackVersions(IEnumerable<int> versions)
        {
            //This is a 'magic' handshake identifier to indicate we're using 'BOLT' ('GOGOBOLT')
            var aLittleBitOfMagic = BitConverter.GetBytes(0x6060B017);

            var bytes = new List<byte>(aLittleBitOfMagic);
            foreach (var version in versions)
            {
                bytes.AddRange(BitConverter.GetBytes(version));
            }
            return bytes.ToArray();
        }


        private static int GetAgreedVersion(byte[] data)
        {
            return BitConverter.ToInt32(data);
        }

        public async Task<Result> Run(string statement, IDictionary<string, object> statementParameters = null)
        {
            /*
            1. Pack statement
            2. Chunk the packed statement
            3. connection.SOMETHING(chunkedData);
            */
            //await TcpSocketClient.WriteStream.WriteAsync(0x21);
            throw new NotImplementedException();
        }
    }
}