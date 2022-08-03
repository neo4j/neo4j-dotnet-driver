using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Neo4j.Driver.Tests.TestBackend
{
	internal class NewSession : IProtocolObject
	{
		public enum SessionState
		{
			RetryAbleNothing,
			RetryAblePositive,
			RetryAbleNegative
		}

		[JsonIgnore]
		public SessionState RetryState { get; private set; } = SessionState.RetryAbleNothing;
		[JsonIgnore]
		public string RetryableErrorId { get; private set; }
		
		public NewSessionType data { get; set; } = new NewSessionType();
		[JsonIgnore]
		public IAsyncSession Session { get; set; }

		public class NewSessionType
		{
			public string driverId { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public string accessMode { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public List<string> bookmarks { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public string database { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public long? fetchSize { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public string impersonatedUser { get; set; }
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

		[JsonIgnore]
		public List<string> SessionTransactions { get; } = new List<string>();

        void SessionConfig(SessionConfigBuilder configBuilder)
        {
            if(!string.IsNullOrEmpty(data.database)) configBuilder.WithDatabase(data.database);            
            if(!string.IsNullOrEmpty(data.accessMode)) configBuilder.WithDefaultAccessMode(GetAccessMode);
            if(data.bookmarks != null) configBuilder.WithBookmarks(Bookmarks.From(data.bookmarks.ToArray()));
            if(data.fetchSize.HasValue)
                configBuilder.WithFetchSize(data.fetchSize.Value);
			if (!string.IsNullOrEmpty(data.impersonatedUser)) configBuilder.WithImpersonatedUser(data.impersonatedUser);
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

		public void SetupRetryAbleState(SessionState state, string retryableErrorId = "")
		{
			RetryState = state;
			RetryableErrorId = retryableErrorId;
		}
    }
}
