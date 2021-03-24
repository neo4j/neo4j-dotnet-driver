using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RequestReader
    {
        private StreamReader InputReader { get; }
        private bool MessageOpen { get; set; }
        private const string OpenTag = "#request begin";
        private const string CloseTag = "#request end";
		
        public string CurrentObjectData { get; set; }

        public RequestReader(StreamReader reader)
        {
            InputReader = reader;            
        }

        public async Task<bool> ParseNextRequest()
        {
            Trace.WriteLine("Listening for request");

            CurrentObjectData = string.Empty;

            while (await ParseObjectData().ConfigureAwait(false)) ;

            Trace.WriteLine($"\nRequest recieved: {CurrentObjectData}");

            return !string.IsNullOrEmpty(CurrentObjectData);
        }

        private async Task<bool> ParseObjectData()
        {
            var input = await InputReader.ReadLineAsync();

            if (string.IsNullOrEmpty(input))
                return false;

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
            return ProtocolObjectFactory.CreateObject(GetObjectType(), CurrentObjectData);
        }

        private string GetObjectTypeName()
        {
            JObject jsonObject = JObject.Parse(CurrentObjectData);
            return (string)jsonObject["name"];
        }

		public Type GetObjectType()
		{	
			return Type.GetType(this.GetType().Namespace + "." + GetObjectTypeName(), true);
		}
    }
}
