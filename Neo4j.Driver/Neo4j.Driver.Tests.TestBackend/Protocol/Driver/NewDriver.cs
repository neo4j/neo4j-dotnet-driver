using System;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SimpleLogger : ILogger
	{
		public void Debug(string message, params Object[] args)
		{
			Console.WriteLine("[DRIVER-DEBUG]" + message, args);
		}
		public void Error(System.Exception error, string message, params Object[] args)
		{
			Console.WriteLine("[DRIVER-ERROR]" + message, args);
		}
		public void Info(string message, params Object[] args)
		{
			Console.WriteLine("[DRIVER-INFO]" + message, args);
		}
		public bool IsDebugEnabled()
		{
			return true;
		}
		public bool IsTraceEnabled()
		{
			return true;
		}
		public void Trace(string message, params Object[] args)
		{
			Console.WriteLine("[DRIVER-TRACE]" + message, args);
		}
		public void Warn(System.Exception error, string message, params Object[] args)
		{
			Console.WriteLine("[DRIVER-WARN]" + message, args);
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
			public int? maxConnectionPoolSize { get; set; }
			public int? connectionAcquisitionTimeoutMs { get; set; }
			public int? sessionConnectionTimeoutMs { get; set; }
			public int? updateRoutingTableTimeoutMs { get; set; }
			public int? fetchSize { get; set; }
			public int? maxTxRetryTimeMs { get; set; }
			public int? livenessCheckTimeoutMs { get; set; }
		}

		public override async Task Process(Controller controller)
		{
			Control = controller;
			var authTokenData = data.authorizationToken.data;

			IAuthToken authToken;

			switch (authTokenData.scheme)
			{
				case "bearer":
					authToken = AuthTokens.Bearer(authTokenData.credentials);
					break;
				case "kerberos":
					authToken = AuthTokens.Kerberos(authTokenData.credentials);
					break;

				default:
					authToken = AuthTokens.Custom(authTokenData.principal,
												  authTokenData.credentials,
												  authTokenData.realm,
												  authTokenData.scheme,
												  authTokenData.parameters);
					break;

			}

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

			if (data.maxConnectionPoolSize.HasValue) configBuilder.WithMaxConnectionPoolSize(data.maxConnectionPoolSize.Value);

			if (data.connectionAcquisitionTimeoutMs.HasValue) configBuilder.WithConnectionAcquisitionTimeout(TimeSpan.FromMilliseconds(data.connectionAcquisitionTimeoutMs.Value));

			if (data.sessionConnectionTimeoutMs.HasValue) throw new TestKitProtocolException("Backend does not know how to handle sessionConnectionTimeoutMs.");

			if (data.updateRoutingTableTimeoutMs.HasValue) throw new TestKitProtocolException("Backend does not know how to handle updateRoutingTableTimeoutMs.");

			SimpleLogger logger = new SimpleLogger();

			configBuilder.WithLogger(logger);
		}
	}
}
