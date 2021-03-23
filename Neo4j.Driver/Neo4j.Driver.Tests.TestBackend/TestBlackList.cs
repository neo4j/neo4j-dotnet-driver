using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	static class TestBlackList
	{
		private static readonly (string Name, string Reason)[] BlackListNames = new []										//TestKit filename
		{ 
			("test_iteration_nested",																						//sessionRun.py
			 "Nested results not working in 4.2 and earlier. FIX AND ENABLE in 4.3"),
			
			("test_iteration_nested",																						//txfuncrun.py
			 "Fails for some reason"),															
			
			("test_disconnect_on_commit", 																					//retry.py
			 "Keeps retrying on commit despite connection being dropped"),
			
			("test_retry_ForbiddenOnReadOnlyDatabase", 																		//retry.py
			 "Behaves strange"),

			("test_retry_NotALeader",																						//retry.py									
			 "Behaves strange"),

			("test_retry_ForbiddenOnReadOnlyDatabase_ChangingWriter", 														//retry.py
			 "Behaves strange"),

			("test_should_write_successfully_on_leader_switch_using_tx_function", 											//routing.py
			 "requires investigation"),

			("test_should_retry_write_until_success_with_leader_change_using_tx_function",									//routing.py
				"requires investigation"),
			
			("test_should_retry_write_until_success_with_leader_shutdown_during_tx_using_tx_function", 						//routing.py
				"requires investigation"),

			("test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code",										//routing.py
				"consume not implemented in backend"),

			("test_should_fail_when_writing_on_writer_that_returns_not_a_leader_code_using_tx_run", 						//routing.py
				"consume not implemented in backend"),

			("test_should_retry_read_tx_until_success", 																	//routing.py
				"requires investigation"),

			("test_should_retry_write_tx_until_success", 																	//routing.py
				"requires investigation"),

			("test_should_retry_read_tx_and_rediscovery_until_success", 													//routing.py
				"requires investigation"),

			("test_should_retry_write_tx_and_rediscovery_until_success", 													//routing.py
				"requires investigation"),

			("test_should_serve_reads_and_fail_writes_when_no_writers_available",											//routing.py
				"consume not implemented in backend or requires investigation"),

			("test_should_forget_address_on_database_unavailable_error", 													//routing.py
				"requires investigation"),

			("test_should_use_resolver_during_rediscovery_when_existing_routers_fail", 										//routing.py
				"resolver not implemented in backend"),

			("test_should_revert_to_initial_router_if_known_router_throws_protocol_errors", 								//routing.py
				"resolver not implemented in backend"),	

			("test_should_read_successfully_on_empty_discovery_result_using_session_run",									//routing.py
				"resolver not implemented in backend"),

			("test_should_fail_with_routing_failure_on_db_not_found_discovery_failure",										//routing.py
				"add code support"),

			("test_1_1",
				"TLS 1.1 is not supported in .Net"), 
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
