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
                "ConfHint:connection.recv_timeout_seconds",
                "AuthorizationExpiredTreatment",
                "Detail:ClosedDriverIsEncrypted",
                "Detail:DefaultSecurityConfigValueEquality",
                "Feature:API:BookmarkManager",
                "Feature:API:ConnectionAcquisitionTimeout",
                "Feature:API:Driver:GetServerInfo",
                "Feature:API:Driver.IsEncrypted",
                "Feature:API:Driver.VerifyConnectivity",
                //"Feature:API:Liveness.Check",
                "Feature:API:Result.List",
                "Feature:API:Result.Peek",
                "Feature:API:Result.Single",
                "Feature:API:SSLConfig",
                "Feature:API:SSLSchemes",
                "Feature:API:Type.Temporal",
                "Feature:Auth:Bearer",
                "Feature:Auth:Custom",
                "Feature:Auth:Kerberos",
                "Feature:Bolt:3.0",
                "Feature:Bolt:4.1",
                "Feature:Bolt:4.2",
                "Feature:Bolt:4.3",
                "Feature:Bolt:4.4",
                "Feature:Bolt:5.0",
                "Feature:Bolt:Patch:UTC",
                "Feature:Impersonation",
                //"Feature:TLS:1.1",
                "Feature:TLS:1.2",
                //"Feature:TLS:1.3",
                //"Optimization:ConnectionReuse",
                "Optimization:EagerTransactionBegin",
                //"Optimization:ImplicitDefaultArguments",
                //"Optimization:MinimalResets",
                "Optimization:PullPipelining"
                //"Optimization:ResultListFetchAll",
            };
        }
    }
}
