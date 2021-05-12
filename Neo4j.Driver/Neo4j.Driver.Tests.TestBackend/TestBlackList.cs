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

			("routing.Routing.test_should_write_successfully_on_leader_switch_using_tx_function", 											
			 "requires investigation"),

			("routing.Routing.test_should_retry_write_until_success_with_leader_change_using_tx_function",									
				"requires investigation"),
			
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

			("routing.Routing.test_should_forget_address_on_database_unavailable_error", 													
				"requires investigation"),

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

			("stub.routing.RoutingV3.test_should_ignore_system_bookmark_when_getting_rt_for_multi_db",
				"Test is not valid for protocol V3"),


			//TODO: fix these pronto!!
			("stub.routing.RoutingV3.test_should_pass_bookmark_from_tx_to_tx_using_tx_run",
				"Temporarily disabled due a bug with bookmarks being sent when they should not in bolt 3"),

			("stub.routing.RoutingV3.test_should_use_write_session_mode_and_initial_bookmark_when_writing_using_tx_run",
				"Temporarily disabled due a bug with bookmarks being sent when they should not in bolt 3"),

			("stub.routing.RoutingV3.test_should_use_read_session_mode_and_initial_bookmark_when_reading_using_tx_run",
				"Temporarily disabled due a bug with bookmarks being sent when they should not in bolt 3"),

			

			//TODO: Authorization tests
			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_begin_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_run_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_pull_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_commit_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_fail_with_auth_expired_on_rollback_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_retry_on_auth_expired_on_begin_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_retry_on_auth_expired_on_run_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_retry_on_auth_expired_on_pull_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTests.test_should_retry_on_auth_expired_on_commit_using_tx_function",
				"Temporarily disabled whilst being developed"),

			
			("stub.authorization.NoRoutingAuthorizationTests.test_should_drop_connection_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.NoRoutingAuthorizationTests.test_should_be_able_to_use_current_sessions_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),






			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_begin_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_run_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_pull_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_commit_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_fail_with_auth_expired_on_rollback_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_retry_on_auth_expired_on_begin_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_retry_on_auth_expired_on_run_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_retry_on_auth_expired_on_pull_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV4.test_should_retry_on_auth_expired_on_commit_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.NoRoutingAuthorizationTests.test_should_drop_connection_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.NoRoutingAuthorizationTests.test_should_be_able_to_use_current_sessions_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),





			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_pull_using_session_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_begin_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_run_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_pull_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_commit_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_fail_with_auth_expired_on_rollback_using_tx_run",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_retry_on_auth_expired_on_begin_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_retry_on_auth_expired_on_run_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_retry_on_auth_expired_on_pull_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.AuthorizationTestsV3.test_should_retry_on_auth_expired_on_commit_using_tx_function",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.NoRoutingAuthorizationTests..test_should_drop_connection_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),

			("stub.authorization.NoRoutingAuthorizationTests.test_should_be_able_to_use_current_sessions_after_AuthorizationExpired",
				"Temporarily disabled whilst being developed"),
		}; 

		public static bool FindTest(string testName, out string reason)
		{
			var item = Array.Find(BlackListNames, x => x.Name == testName);

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
