using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewSession : IProtocolObject
    {
        public NewSessionType data { get; set; } = new NewSessionType();
        [JsonIgnore]
        public IAsyncSession Session { get; set; }

        public class NewSessionType
        {
            public string driverId { get; set; }
            
            [JsonProperty(Required = Required.AllowNull)]
            public string accessMode { get; set; }
            
            [JsonProperty(Required = Required.AllowNull)]
            public List<string> bookmarks { get; set; } = new List<string>();
           
            [JsonProperty(Required = Required.AllowNull)]
            public string database { get; set; }
            
            [JsonProperty(Required = Required.AllowNull)]
            public long fetchSize { get; set; } = Constants.DefaultFetchSize;
        }

        [JsonIgnore]
        public AccessMode GetAccessMode
        {
            get
            {
                if (data.accessMode == "r")
                    return AccessMode.Read;
                else
                    return AccessMode.Write;
            }
        }

        void SessionConfig(SessionConfigBuilder configBuilder)
        {
            if(!string.IsNullOrEmpty(data.database)) configBuilder.WithDatabase(data.database);            
            if(!string.IsNullOrEmpty(data.accessMode)) configBuilder.WithDefaultAccessMode(GetAccessMode);
            if(data.bookmarks.Count > 0) configBuilder.WithBookmarks(Bookmark.From(data.bookmarks.ToArray()));
            configBuilder.WithFetchSize(data.fetchSize);

            configBuilder.Build();
        }

        public override async Task Process()
        {   
            IDriver driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;

            Session = driver.AsyncSession(SessionConfig);

            await Task.CompletedTask;
        }

        public override string Respond()
        {  
            return new ProtocolResponse("Session", uniqueId).Encode();
        }
    }
}
