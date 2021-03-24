using System;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;


namespace Neo4j.Driver.Tests.TestBackend
{
	internal class ListAddressResolver : IServerAddressResolver
	{
		private readonly ServerAddress[] servers;
		Controller Control { get; }
		Uri Uri { get; }

		public ListAddressResolver(Controller control, string uri)
		{
			Control = control;
			Uri = new Uri(uri);
		}

		public ListAddressResolver(params ServerAddress[] servers)
		{
			this.servers = servers;
		}

		public ISet<ServerAddress> Resolve(ServerAddress address)
		{
			string errorMessage = "A ResolverResolutionCompleted request is expected straight after a ResolverResolutionRequired reponse is sent";
			var response = new ProtocolResponse("ResolverResolutionRequired",
												new
												{
													id = ProtocolObjectManager.GenerateUniqueIdString(),
													address = Uri.Host + ":" + Uri.Port
												})
												.Encode();

			//Send the ResolverResolutionRequired response
			Control.SendResponse(response).ConfigureAwait(false);

			//Read the ResolverResolutionCompleted request, throw if another type of request has come in
			var result = Control.TryConsumeStreamObjectOfType<ResolverResolutionCompleted>().Result;
			if(result is null)
				throw new NotSupportedException(errorMessage);

			//Return a IServerAddressResolver instance thats Resolve method uses the addresses in the ResolverResolutionoCompleted request.
			return new HashSet<ServerAddress>(result
											  .data
											  .addresses
											  .Select(x =>
											  {
												  string[] split = x.Split(':');
												  return ServerAddress.From(split[0], Convert.ToInt32(split[1]));
											  }));
		}
	}

	internal class NewDriver : IProtocolObject
	{
		public NewDriverType data { get; set; } = new NewDriverType();
		[JsonIgnore]
		public IDriver Driver { get; set; }
		[JsonIgnore]
		private Controller Control { get; set; }


		public class NewDriverType
		{
			public string uri { get; set; }
			public AuthorizationToken authorizationToken { get; set; } = new AuthorizationToken();
			public string userAgent { get; set; }
			public bool resolverRegistered { get; set; } = false;
			public bool domainNameResolverRegistered { get; set; } = false;
			public int connectionTimeoutMs { get; set; } = -1;
		}

		public override async Task Process(Controller controller)
		{
			Control = controller;
			var authTokenData = data.authorizationToken.data;
			var authToken = AuthTokens.Custom(authTokenData.principal, authTokenData.credentials, authTokenData.realm, authTokenData.scheme);

			Driver = GraphDatabase.Driver(data.uri, authToken, DriverConfig);

			await Task.CompletedTask;
		}

		public override string Respond()
		{
			return new ProtocolResponse("Driver", uniqueId).Encode();
		}

		private void DriverConfig(ConfigBuilder configBuilder)
		{
			if (!string.IsNullOrEmpty(data.userAgent)) configBuilder.WithUserAgent(data.userAgent);

			if (data.resolverRegistered) configBuilder.WithResolver(new ListAddressResolver(Control, data.uri));

			if (data.connectionTimeoutMs > 0) configBuilder.WithConnectionTimeout(TimeSpan.FromMilliseconds(data.connectionTimeoutMs));
		}
	}
}
