using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class SupportedFeatures
    {
        public static IReadOnlyList<string> FeaturesList { get; }

        static SupportedFeatures()
        {
            FeaturesList = new List<string>
            {
                //"Feature:API:Result.List",
                "Feature:API:Result.Peek",
                "Feature:API:Result.Single",
                //"Feature:API:Liveness.Check",
                //"Feature:API:SSLConfig",
                //"Feature:API:SSLSchemes",
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
                //"Feature:TLS:1.1",
                "Feature:TLS:1.2",
                //"Feature:TLS:1.3",
                "AuthorizationExpiredTreatment",
                //"Optimization:ConnectionReuse",
                "Optimization:EagerTransactionBegin",
                //"Optimization:ImplicitDefaultArguments",
                //"Optimization:MinimalResets",
                //"Optimization:PullPipelining",
                //"Optimization:ResultListFetchAll",
                "ConfHint:connection.recv_timeout_seconds",
                //"Temporary:ConnectionAcquisitionTimeout",
                //"Temporary:CypherPathAndRelationship",
                //"Temporary:DriverFetchSize",
                //"Temporary:DriverMaxConnectionPoolSize",
                //"Temporary:DriverMaxTxRetryTime",
                //"Temporary:FastFailingDiscovery",
                "Temporary:FullSummary",
                //"Temporary:GetConnectionPoolMetrics",
                //"Temporary:ResultKeys",
                //"Temporary:TransactionClose"
            };
        }
    }
}
