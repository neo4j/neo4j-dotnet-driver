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
			("sessionrun.TestSessionRun.test_iteration_nested",
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


			("routing.Routing.test_should_retry_write_until_success_with_leader_shutdown_during_tx_using_tx_function",
				"requires investigation"),

			("routing.Routing.test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code",
				"consume not implemented in backend"),

			("routing.Routing.test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code_using_tx_run",
				"consume not implemented in backend"),

			("routing.Routing.test_should_retry_read_tx_until_success",
				"requires investigation"),

			("routing.Routing.test_should_retry_write_tx_until_success",
				"requires investigation"),

			("routing.Routing.test_should_retry_read_tx_and_rediscovery_until_success",
				"requires investigation"),

			("routing.Routing.test_should_retry_write_tx_and_rediscovery_until_success",
				"requires investigation"),

			("routing.Routing.test_should_serve_reads_and_fail_writes_when_no_writers_available",
				"consume not implemented in backend or requires investigation"),

			("routing.Routing.test_should_use_resolver_during_rediscovery_when_existing_routers_fail",
				"resolver not implemented in backend"),

			("routing.Routing.test_should_revert_to_initial_router_if_known_router_throws_protocol_errors",
				"resolver not implemented in backend"),

			("routing.Routing.test_should_read_successfully_on_empty_discovery_result_using_session_run",
				"resolver not implemented in backend"),

			("routing.Routing.test_should_fail_with_routing_failure_on_db_not_found_discovery_failure",
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

			("stub.driver_parameters.test_connection_acquisition_timeout_ms.TestConnectionAcquisitionTimeoutMs.test_does_not_encompass_router_handshake",
				"TODO: ConnectionAcquisitionTimeout cancels handshake with the router"),

			// Replacing temporary feature flags
			("test_should_accept_custom_fetch_size_using_driver_configuration",
				"TMP_DRIVER_FETCH_SIZE"),
			("test_should_pull_all_when_fetch_is_minus_one_using_driver_configuration",
				"TMP_DRIVER_FETCH_SIZE"),
			("test_should_fail_when_reading_from_unexpectedly_interrupting_readers_on_run_using_tx_function",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_fail_when_reading_from_unexpectedly_interrupting_readers_using_tx_function",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_fail_when_writing_on_unexpectedly_interrupting_writer_on_pull_using_session_run",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_fail_with_routing_failure_on_invalid_bookmark_discovery_failure",
				"TMP_FAST_FAILING_DISCOVERY"),
			("test_should_fail_with_routing_failure_on_invalid_bookmark_mixture_discovery_failure",
				"TMP_FAST_FAILING_DISCOVERY"),
			("test_should_fail_with_routing_failure_on_forbidden_discovery_failure",
				"TMP_FAST_FAILING_DISCOVERY"),
			("test_should_fail_with_routing_failure_on_any_security_discovery_failure",
				"TMP_FAST_FAILING_DISCOVERY"),
			("test_should_fail_when_writing_to_unexpectedly_interrupting_writers_using_tx_function",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_fail_when_writing_to_unexpectedly_interrupting_writers_on_run_using_tx_function",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_request_rt_from_all_initial_routers_until_successful_on_unknown_failure",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_request_rt_from_all_initial_routers_until_successful_on_authorization_expired",
				"TMP_DRIVER_MAX_TX_RETRY_TIME"),
			("test_should_drop_connections_failing_liveness_check",
				"API_LIVENESS_CHECK, TMP_GET_CONNECTION_POOL_METRICS"),
			("test_can_return_relationship",
				"TMP_CYPHER_PATH_AND_RELATIONSHIP"),
			("test_can_return_path",
				"TMP_CYPHER_PATH_AND_RELATIONSHIP"),
			("test_supports_multi_db",
				"TMP_FULL_SUMMARY"),
			("test_multi_db",
				"TMP_FULL_SUMMARY"),
			("test_can_obtain_summary_after_consuming_result",
				"TMP_FULL_SUMMARY"),
			("test_no_plan_info",
				"TMP_FULL_SUMMARY"),
			("test_can_obtain_plan_info",
				"TMP_FULL_SUMMARY"),
			("test_can_obtain_profile_info",
				"TMP_FULL_SUMMARY"),
			("test_no_notification_info",
				"TMP_FULL_SUMMARY"),
			("test_can_obtain_notification_info",
				"TMP_FULL_SUMMARY"),
			("test_contains_time_information",
				"TMP_FULL_SUMMARY"),
			("test_protocol_version_information",
				"TMP_FULL_SUMMARY"),
			("test_summary_counters_case_",  // ...case_1 and ...case_2
				"TMP_FULL_SUMMARY"),
			("test_server_info",
				"TMP_FULL_SUMMARY"),
			("test_database",
				"TMP_FULL_SUMMARY"),
			("test_query",
				"TMP_FULL_SUMMARY"),
			("test_invalid_query_type",
				"TMP_FULL_SUMMARY"),
			("test_times",
				"TMP_FULL_SUMMARY"),
			("test_no_times",
				"TMP_FULL_SUMMARY"),
			("test_no_notifications",
				"TMP_FULL_SUMMARY"),
			("test_empty_notifications",
				"TMP_FULL_SUMMARY"),
			("test_full_notification",
				"TMP_FULL_SUMMARY"),
			("test_notifications_without_position",
				"TMP_FULL_SUMMARY"),
			("test_multiple_notifications",
				"TMP_FULL_SUMMARY"),
			("test_plan",
				"TMP_FULL_SUMMARY"),
			("test_profile",
				"TMP_FULL_SUMMARY"),
			("test_empty_summary",
				"TMP_FULL_SUMMARY"),
			("test_full_summary_no_flags",
				"TMP_FULL_SUMMARY"),
			("test_no_summary",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_constraints_added",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_constraints_removed",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_contains_system_updates",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_contains_updates",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_not_contains_system_updates",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_not_contains_updates",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_indexes_added",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_indexes_removed",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_labels_added",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_labels_removed",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_nodes_created",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_nodes_deleted",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_properties_set",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_relationships_created",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_relationships_deleted",
				"TMP_FULL_SUMMARY"),
			("test_partial_summary_system_updates",
				"TMP_FULL_SUMMARY"),
			("test_should_error_on_rollback_failure_using_tx_close",
				"TMP_TRANSACTION_CLOSE"),
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
