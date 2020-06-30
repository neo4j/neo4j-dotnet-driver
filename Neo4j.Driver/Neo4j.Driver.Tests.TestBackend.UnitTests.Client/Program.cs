using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Neo4j.Driver.Tests.TestBackend.UnitTests.Client

{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Thread.Sleep(5000);
            

            var port = 9876;            
            var client = new TcpClient("127.0.0.1", port);
            var stream = client.GetStream();
            var writer = new StreamWriter(stream, Encoding.ASCII);

            Console.WriteLine($"Starting NutKitDotNetTestClient on {client}:{port}");

            string request = "#request begin\n{\"name\": \"NewDriver\", \"data\": {\"uri\": \"bolt://neo4jserver:7687\", \"authorizationToken\": {\"name\": \"AuthorizationToken\", \"data\": {\"scheme\": \"basic\", \"principal\": \"neo4j\", \"credentials\": \"pass\", \"realm\": \"\", \"ticket\": \"\"}}}}\n#request end";
            await writer.WriteLineAsync(request);
            await writer.FlushAsync();
            await stream.FlushAsync();

            await ReadResponse(stream, writer);
            
            string request2 = "#request begin\n{\"name\": \"NewSession\", \"data\": {\"driverId\": \"0\", \"accessMode\": \"w\", \"bookmarks\": null}}\n#request end";
            await writer.WriteLineAsync(request2);
            await writer.FlushAsync();
            await stream.FlushAsync();
            
            await ReadResponse(stream, writer);

            writer.Close();
            stream.Close();
            client.Close();

        }

        private static async Task ReadResponse(Stream stream, StreamWriter writer)
        {
            var reader = new StreamReader(stream, Encoding.ASCII);

            var tmpString = "";
            var resultString = "";
            while ((tmpString = await reader.ReadLineAsync()) != null)
            {
                resultString += tmpString;

                if (tmpString == "#response end")
                    break;
            }

            Console.WriteLine(resultString);
        }
    }
}
