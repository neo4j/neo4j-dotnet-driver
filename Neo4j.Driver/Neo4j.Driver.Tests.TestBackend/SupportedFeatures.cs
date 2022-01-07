using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal static class SupportedFeatures
	{
		public static List<string> FeaturesList { get; }

		static SupportedFeatures()
		{
			FeaturesList = new List<string>
			{
				"AuthorizationExpiredTreatment",
				"Feature:API:Result.Peek",
				"Feature:API:Result.List",
				"Feature:Auth:Bearer",
				"Feature:Auth:Custom",
				"Feature:Auth:Kerberos",
				"Feature:Bolt:3.0",
				"Feature:Bolt:4.0",
				"Feature:Bolt:4.1",
				"Feature:Bolt:4.2",
				"Feature:Bolt:4.3",
				"Feature:Bolt:4.4",
				"Feature:Impersonation",
				"Feature:TLS:1.2",
				"Optimization:ConnectionReuse",
				"Optimization:EagerTransactionBegin",
				"Optimization:ImplicitDefaultArguments",
				"Optimization:MinimalResets",
				"Optimization:PullPipelining",
				"ConfHint:connection.recv_timeout_seconds",
				"Temporary:ConnectionAcquisitionTimeout",
				"Temporary:CypherPathAndRelationship",
				"Temporary:DriverFetchSize",
				"Temporary:DriverMaxConnectionPoolSize",
				"Temporary:DriverMaxTxRetryTime",
				"Temporary:FastFailingDiscovery",
				"Temporary:FullSummary",
				"Temporary:GetConnectionPoolMetrics",
				"Temporary:ResultKeys",
				"Temporary:TransactionClose"
			};
		}
	}
}
