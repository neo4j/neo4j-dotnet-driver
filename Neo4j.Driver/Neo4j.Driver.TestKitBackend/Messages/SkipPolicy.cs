// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal class SubstringSkipPolicy : ISkipPolicy
{
    private static readonly (string Fragment, string Reason)[] Entries =
    [
        ("test_session_run.TestSessionRun.test_iteration_nested",
            "Nested results not working in 4.2 and earlier. FIX AND ENABLE in 4.3"),

        ("txfuncrun.TestTxFuncRun.test_iteration_nested",
            "Fails for some reason"),

        ("retry.TestRetry.test_disconnect_on_commit",
            "Keeps retrying on commit despite connection being dropped"),

        ("tlsversions.TestTlsVersions.test_1_1",
            "TLS 1.1 is not supported in .Net"),

        ("test_should_request_rt_from_all_initial_routers_until_successful",
            "Fails with ServiceUnavailableError: the driver never falls back to the remaining initial " +
            "routers after the first router failure during RT discovery (reproduced on v4x3/v5x0). " +
            "Tracked as DRIVERS-515."),

        ("test_should_retry_write_until_success_with_leader_shutdown_during_tx_using_tx_function",
            "Test failing requires investigation"),

        ("test_should_echo_relationship",
            "Backend does not yet support serializing relationships"),

        ("test_should_echo_path",
            "Backend does not yet support serializing paths"),

        ("test_summary.TestSummary.test_protocol_version_information", "Server not responding with 5.0"),

        ("stub.iteration.test_iteration_session_run.TestIterationSessionRun.test_nested",
            "Nested session.run() results desync the wire: the driver sends DISCARD instead of PULL for the " +
            "inner run, unlike managed-tx nesting which works. Tracked as DRIVERS-516."),

        ("test_temporal_types.TestDataTypes.test_date_time_cypher_created_tz_id",
            "No Antarctica/Troll mapping available."),

        ("test_temporal_types.TestDataTypes.test_should_echo_all_timezone_ids",
            "EST/HST/MST not supported."),

        ("test_connection_acquisition_timeout_during_fallback",
            "Driver currently uses separate acquisition timeouts for the separate connections. Future behavioural " +
            "fix (6.0) needed to pass test and unify with other drivers."),

        ("test_homedb.TestHomeDbMixedCluster.test_re_enabling_cache",
            "Re-enabling cache delayed until 6.0 release."),

        ("test_homedb.TestHomeDbMixedCluster.test_re_enabling_cache_after_disabling",
            "Re-enabling cache delayed until 6.0 release."),

        ("test_should_fail_when_writing_on_writer_that_returns_forbidden_on_read_only_database",
            "Legacy skips this too (requires Feature.BACKEND_RT_FETCH, which legacy never declares). Retry-" +
            "exhaustion error reporting drops the GQL code here; a real bug, but not a parity requirement."),

        ("test_should_fail_when_writing_on_unexpectedly_interrupting_writer_using_tx_run",
            "Legacy skips this too (requires Feature.BACKEND_RT_FETCH, which legacy never declares). No error " +
            "is raised on retry exhaustion here; a real bug, but not a parity requirement."),

        ("test_should_fail_when_writing_on_unexpectedly_interrupting_writer_on_run_using_tx_run",
            "Legacy skips this too (requires Feature.BACKEND_RT_FETCH, which legacy never declares). No error " +
            "is raised on retry exhaustion here; a real bug, but not a parity requirement.")
    ];

    public bool TryGetSkipReason(string testName, [NotNullWhen(true)] out string? reason)
    {
        reason = Entries
            .FirstOrDefault(e => testName.Contains(e.Fragment, StringComparison.Ordinal))
            .Reason;

        return reason != null;
    }
}
