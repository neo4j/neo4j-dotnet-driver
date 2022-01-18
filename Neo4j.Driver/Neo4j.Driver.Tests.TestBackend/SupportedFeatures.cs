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
                // The driver offers a method for the result to return all records as a list
                // or array. This method should exhaust the result.
                "Feature:API:Result.List",
                // The driver offers a method for the result to peek at the next record in
                // the result stream without advancing it (i.e. without consuming any
                // records)
                "Feature:API:Result.Peek",
                // The driver offers a method for the result to retrieve exactly one record.
                // This methods asserts that exactly one record in left in the result
                // stream, else it will raise an exception.
                "Feature:API:Result.Single",
                // The driver supports connection liveness check.
                "Feature:API:Liveness.Check",
                // The driver implements explicit configuration options for SSL.
                //  - enable / disable SSL
                //  - verify signature against system store / custom cert / not at all
                "Feature:API:SSLConfig",
                // The driver understands bolt+s, bolt+ssc, neo4j+s, and neo4j+ssc schemes
                // and will configure its ssl options automatically.
                // ...+s: enforce SSL + verify  server's signature with system's trust store
                // ...+ssc: enforce SSL but do not verify the server's signature at all
                "Feature:API:SSLSchemes",
                // The driver supports single-sign-on (SSO) by providing a bearer auth token
                // API.
                "Feature:Auth:Bearer",
                // The driver supports custom authentication by providing a dedicated auth
                // token API.
                "Feature:Auth:Custom",
                // The driver supports Kerberos authentication by providing a dedicated auth
                // token API.
                "Feature:Auth:Kerberos",
                // The driver supports Bolt protocol version 3
                "Feature:Bolt:3.0",
                // The driver supports Bolt protocol version 4.0
                "Feature:Bolt:4.0",
                // The driver supports Bolt protocol version 4.1
                "Feature:Bolt:4.1",
                // The driver supports Bolt protocol version 4.2
                "Feature:Bolt:4.2",
                // The driver supports Bolt protocol version 4.3
                "Feature:Bolt:4.3",
                // The driver supports Bolt protocol version 4.4
                "Feature:Bolt:4.4",
                // The driver supports impersonation
                "Feature:Impersonation",
                // The driver supports TLS 1.1 connections.
                // If this flag is missing, TestKit assumes that attempting to establish
                // such a connection fails.
                "Feature:TLS:1.1",
                // The driver supports TLS 1.2 connections.
                // If this flag is missing, TestKit assumes that attempting to establish
                // such a connection fails.
                "Feature:TLS:1.2",
                // The driver supports TLS 1.3 connections.
                // If this flag is missing, TestKit assumes that attempting to establish
                // such a connection fails.
                "Feature:TLS:1.3",
                // === OPTIMIZATIONS ===
                // On receiving Neo.ClientError.Security.AuthorizationExpired, the driver
                // shouldn't reuse any open connections for anything other than finishing
                // a started job. All other connections should be re-established before
                // running the next job with them.
                "AuthorizationExpiredTreatment",
                // The driver caches connections (e.g., in a pool) and doesn't start a new
                // one (with hand-shake, HELLO, etc.) for each query.
                "Optimization:ConnectionReuse",
                // The driver first tries to SUCCESSfully BEGIN a transaction before calling
                // the user-defined transaction function. This way, the (potentially costly)
                // transaction function is not started until a working transaction has been
                // established.
                "Optimization:EagerTransactionBegin",
                // Driver doesn't explicitly send message data that is the default value.
                // This conserves bandwidth.
                "Optimization:ImplicitDefaultArguments",
                // The driver sends no more than the strictly necessary RESET messages.
                "Optimization:MinimalResets",
                // The driver doesn't wait for a SUCCESS after calling RUN but pipelines a
                // PULL right afterwards and consumes two messages after that. This saves a
                // full round-trip.
                "Optimization:PullPipelining",
                // This feature requires `API_RESULT_LIST`.
                // The driver pulls all records (`PULL -1`) when Result.list() is called.
                // (As opposed to iterating over the Result with the configured fetch size.)
                // Note: If your driver supports this, make sure to document well that this
                //       method ignores the configures fetch size. Your users will
                //       appreciate it <3.
                "Optimization:ResultListFetchAll",
                // === CONFIGURATION HINTS (BOLT 4.3+) ===
                // The driver understands and follow the connection hint
                // connection.recv_timeout_seconds which tells it to close the connection
                // after not receiving an answer on any request for longer than the given
                // time period. On timout, the driver should remove the server from its
                // routing table and assume all other connections to the server are dead
                // as well.
                "ConfHint:connection.recv_timeout_seconds",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented the connection acquisition timeout config.
                "Temporary:ConnectionAcquisitionTimeout",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented path and relationship types
                "Temporary:CypherPathAndRelationship",
                // TODO Update this once the decision has been made.
                // Temporary driver feature. There is a pending decision on whether it
                // should be supported in all drivers or be removed from all of them.
                "Temporary:DriverFetchSize",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented the max connection pool size config.
                "Temporary:DriverMaxConnectionPoolSize",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented it.
                "Temporary:DriverMaxTxRetryTime",
                // Temporary driver feature that will be removed when all official driver
                // implemented failing fast and surfacing on certain error codes during
                // discovery (initial fetching of a RT).
                "Temporary:FastFailingDiscovery",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented all summary response fields.
                "Temporary:FullSummary",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented the GetConnectionPoolMetrics request.
                "Temporary:GetConnectionPoolMetrics",
                // Temporary driver feature that will be removed when all official drivers
                // have been unified in their behaviour of when they return a Result object.
                // We aim for drivers to not providing a Result until the server replied
                // with SUCCESS so that the result keys are already known and attached to
                // the Result object without further waiting or communication with the
                // server.
                "Temporary:ResultKeys",
                // Temporary driver feature that will be removed when all official driver
                // backends have implemented the TransactionClose request
                "Temporary:TransactionClose"
            };
		}
	}
}
