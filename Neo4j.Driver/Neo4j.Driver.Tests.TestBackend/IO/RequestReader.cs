using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RequestReader
    {
        private Reader InputReader { get; }
        private bool MessageOpen { get; set; }
        private ProtocolObjectFactory ProtocolFactory { get; set; }

        private const string OpenTag = "#request begin";
        private const string CloseTag = "#request end";

        public string CurrentObjectData { get; set; }

        public RequestReader(Reader reader, ProtocolObjectFactory factory)
        {
            InputReader = reader;
            ProtocolFactory = factory;
        }

        public async Task<IProtocolObject> ParseNextRequest()
        {
            CurrentObjectData = string.Empty;

            while (await ParseObjectData().ConfigureAwait(false)) { }

            Trace.WriteLine($"\nRequest recieved: {CurrentObjectData}");
            return CreateObjectFromData();
        }

        /*
        private async Task<bool> ParseObjectData()
        {
            var input = await InputReader.Read().ConfigureAwait(false);
            
            if (string.IsNullOrEmpty(input))
                return false;

            input = input.TrimStart();
            input = input.TrimEnd();

            if (IsOpenTag(input))
                return true;

            if (IsCloseTag(input))
                return false;

            CurrentObjectData += input;
            return true;
        }
        */

        private async Task<bool> ParseObjectData()
        {
            var input = await InputReader.Read().ConfigureAwait(false) ?? "";

            input = input.TrimStart();
            input = input.TrimEnd();
            
            if (string.IsNullOrEmpty(input))
                return true;

            if (IsOpenTag(input))
                return true;

             if (IsCloseTag(input))
                 return false;

            if (MessageOpen)
                CurrentObjectData += input;

             return true;

        }

        private bool IsOpenTag(string input)
        {
            if (input == OpenTag)
            {
                if (MessageOpen)
                    throw new IOException($"Read {OpenTag}, but message already open");

                MessageOpen = true;
                return true;
            }

            return false;

        }

        private bool IsCloseTag(string input)
        {
            if (input == CloseTag)
            {
                if (!MessageOpen)
                    throw new IOException($"Read {CloseTag}, but message already closed");

                MessageOpen = false;
                return true;
            }

            return false;
        }

        public IProtocolObject CreateObjectFromData()
        {
            return ProtocolFactory.CreateObject(GetObjectType(), CurrentObjectData);
        }

        private string GetObjectTypeName()
        {
            using var jsonDoc = JsonDocument.Parse(CurrentObjectData);
            var root = jsonDoc.RootElement;
            return root.GetProperty("name").GetString();
        }

        private Protocol.Types GetObjectType()
        {
            var name = GetObjectTypeName();
            return Protocol.Type(name);
        }
    }
}
