using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	static class TestBlackList
	{
		private static readonly (string Name, string Reason)[] BlackListNames = new []
		{
			("test_session_run.TestSessionRun.test_iteration_nested",
			 "Nested results not working in 4.2 and earlier. FIX AND ENABLE in 4.3"),

			("txfuncrun.TestTxFuncRun.test_iteration_nested",
			 "Fails for some reason"),

			("retry.TestRetry.test_disconnect_on_commit",
			 "Keeps retrying on commit despite connection being dropped"),

			("retry.TestRetry.test_retry_ForbiddenOnReadOnlyDatabase",
			 "Behaves strange"),

			("retry.TestRetry.test_retry_NotALeader",
			 "Behaves strange"),

			("retry.TestRetry.test_retry_ForbiddenOnReadOnlyDatabase_ChangingWriter",
			 "Behaves strange"),


			("routing.test_routing_v4x3.RoutingV4x3.test_should_retry_write_until_success_with_leader_shutdown_during_tx_using_tx_function",
				"requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code",
				"consume not implemented in backend"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code_using_tx_run",
				"consume not implemented in backend"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_retry_read_tx_until_success",
				"requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_retry_write_tx_until_success",
				"requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_retry_read_tx_and_rediscovery_until_success",
				"requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_retry_write_tx_and_rediscovery_until_success",
				"requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_serve_reads_and_fail_writes_when_no_writers_available",
				"consume not implemented in backend or requires investigation"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_use_resolver_during_rediscovery_when_existing_routers_fail",
				"resolver not implemented in backend"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_revert_to_initial_router_if_known_router_throws_protocol_errors",
				"resolver not implemented in backend"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_read_successfully_on_empty_discovery_result_using_session_run",
				"resolver not implemented in backend"),

			("routing.test_routing_v4x3.RoutingV4x3.test_should_fail_with_routing_failure_on_db_not_found_discovery_failure",
				"add code support"),

			("tlsversions.TestTlsVersions.test_1_1",
				"TLS 1.1 is not supported in .Net"),




			//TODO:
			("RoutingV3.test_should_ignore_system_bookmark_when_getting_rt_for_multi_db",
				"Test is not valid for protocol V3"),

			("RoutingV3.test_should_use_write_session_mode_and_initial_bookmark_when_writing_using_tx_run",
				"Temporarily disabled due a bug with bookmarks being sent when they should not in bolt 3"),

			("RoutingV3.test_should_use_read_session_mode_and_initial_bookmark_when_reading_using_tx_run",
				"Temporarily disabled due a bug with bookmarks being sent when they should not in bolt 3"),

			/*FAILED From local full testkit docker run against 4.2-cluster*/
			("test_routing_v3.RoutingV3.test_should_accept_routing_table_without_writers_and_then_rediscover",
				"Test failing requires investigation"),
			("test_routing_v3.RoutingV3.test_should_pass_bookmark_from_tx_to_tx_using_tx_run",
				"Test failing requires investigation"),
			("test_routing_v3.RoutingV3.test_should_successfully_send_multiple_bookmarks",
				"Test failing requires investigation"),

			("test_should_revert_to_initial_router_if_known_router_throws_protocol_errors",
				"Test failing requires investigation"),
			("test_should_request_rt_from_all_initial_routers_until_successful",
				"Test failing requires investigation"),
			("test_should_retry_write_until_success_with_leader_change_using_tx_function",
				"Test failing requires investigation"),
			("test_should_retry_write_until_success_with_leader_shutdown_during_tx_using_tx_function",
				"Test failing requires investigation"),


			("test_should_echo_relationship",
				"Backend does not yet support serializing relationships"),
			("test_should_echo_path",
				"Backend does not yet support serializing paths"),

			("stub.tx_lifetime.test_tx_lifetime.TestTxLifetime.test_managed_tx_raises_tx_managed_exec",
				"Driver (still) allows explicit managing of managed transaction"),

            ("test_summary.TestSummary.test_protocol_version_information", "Server not responding with 5.0"),

            ("stub.iteration.test_iteration_tx_run.TestIterationTxRun.test_nested", "Requires further investigation"),
            ("stub.iteration.test_iteration_session_run.TestIterationSessionRun.test_nested",
	            "Requires further investigation"),

			("stub.driver_parameters.test_connection_acquisition_timeout_ms.TestConnectionAcquisitionTimeoutMs.test_does_not_encompass_router_handshake",
				"TODO: ConnectionAcquisitionTimeout cancels handshake with the router")
		};

		public static bool FindTest(string testName, out string reason)
		{
			var item = Array.Find(BlackListNames, x => testName.Contains(x.Name));

			if (item != default)
			{
				reason = item.Reason;
				return true;
			}

			reason = string.Empty;
			return false;
		}
	}
}
