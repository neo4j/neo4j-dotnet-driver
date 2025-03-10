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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.ExceptionHandling;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Preview.GqlErrors;
using Xunit;

namespace Neo4j.Driver.Tests.GqlCompliance;

public class GqlErrorTests
{
    [Fact]
    public void BuildFailureMessage_ShouldDeserializeCorrectly()
    {
        // Arrange
        var values = new Dictionary<string, object>
        {
            { "code", "Neo.ClientError.Transaction.Terminated" },
            { "message", "Transaction terminated" },
            { "gql_status", "GQL_STATUS" },
            { "description", "Status description" },
            { "diagnostic_record", new Dictionary<string, object> { { "key", "value" } } },
            {
                "cause",
                new Dictionary<string, object>
                {
                    { "code", "Neo.ClientError.Transaction.LockClientStopped" }, { "message", "Lock client stopped" }
                }
            }
        };

        var majorVersion = 4;

        // Act
        var result = FailureMessageSerializer.BuildFailureMessage(values, majorVersion);

        // Assert
        result.Code.Should().Be("Neo.ClientError.Transaction.Terminated");
        result.Message.Should().Be("Transaction terminated");
        result.GqlStatus.Should().Be("GQL_STATUS");
        result.GqlStatusDescription.Should().Be("Status description");
        result.GqlDiagnosticRecord.Should().NotBeNull();
        result.GqlDiagnosticRecord["key"].Should().Be("value");
        result.GqlCause.Should().NotBeNull();
        result.GqlCause.Code.Should().Be("Neo.ClientError.Transaction.LockClientStopped");
        result.GqlCause.Message.Should().Be("Lock client stopped");

        // Assert defaults added by FillGqlDefaults
        result.GqlDiagnosticRecord.Should().ContainKey("OPERATION");
        result.GqlDiagnosticRecord["OPERATION"].Should().Be("");
        result.GqlDiagnosticRecord.Should().ContainKey("OPERATION_CODE");
        result.GqlDiagnosticRecord["OPERATION_CODE"].Should().Be("0");
        result.GqlDiagnosticRecord.Should().ContainKey("CURRENT_SCHEMA");
        result.GqlDiagnosticRecord["CURRENT_SCHEMA"].Should().Be("/");

        result.GqlRawClassification.Should().Be(null);
        result.GqlClassification.Should().Be("UNKNOWN");
    }

    [Fact]
    public void GetException_ShouldHandleNestedFailureMessages()
    {
        // Arrange
        var innerFailureMessage = new FailureMessage
        {
            Code = "Neo.ClientError.Transaction.LockClientStopped",
            Message = "Lock client stopped",
            GqlStatus = "GQL_STATUS_INNER",
            GqlStatusDescription = "Inner status description",
            GqlDiagnosticRecord = new Dictionary<string, object> { { "inner_key", "inner_value" } },
            GqlRawClassification = "INNER_CLASSIFICATION",
            GqlClassification = "CLIENT_ERROR"
        };

        var outerFailureMessage = new FailureMessage
        {
            Code = "Neo.ClientError.Transaction.Terminated",
            Message = "Transaction terminated",
            GqlStatus = "GQL_STATUS_OUTER",
            GqlStatusDescription = "Outer status description",
            GqlDiagnosticRecord = new Dictionary<string, object> { { "outer_key", "outer_value" } },
            GqlRawClassification = "OUTER_CLASSIFICATION",
            GqlClassification = "DATABASE_ERROR",
            GqlCause = innerFailureMessage
        };

        var exceptionFactory = new Neo4jExceptionFactory();

        // Act
        var result = exceptionFactory.GetException(outerFailureMessage);

        // Assert
        result.Code.Should().Be("Neo.ClientError.Transaction.Terminated");
        result.Message.Should().Be("Transaction terminated");

        var gqlError = result.GetGqlErrorPreview();
        gqlError.GqlStatus.Should().Be("GQL_STATUS_OUTER");
        gqlError.GqlStatusDescription.Should().Be("Outer status description");
        gqlError.GqlDiagnosticRecord.Should().NotBeNull();
        gqlError.GqlDiagnosticRecord["outer_key"].Should().Be("outer_value");
        gqlError.GqlRawClassification.Should().Be("OUTER_CLASSIFICATION");
        gqlError.GqlClassification.Should().Be("DATABASE_ERROR");

        var innerException = (Neo4jException)result.InnerException;
        gqlError = innerException.GetGqlErrorPreview();
        innerException.Should().NotBeNull();
        innerException.Should().BeOfType<ClientException>();
        innerException.Code.Should().Be("Neo.ClientError.Transaction.LockClientStopped");
        innerException.Message.Should().Be("Lock client stopped");
        gqlError.GqlStatus.Should().Be("GQL_STATUS_INNER");
        gqlError.GqlStatusDescription.Should().Be("Inner status description");
        gqlError.GqlDiagnosticRecord.Should().NotBeNull();
        gqlError.GqlDiagnosticRecord["inner_key"].Should().Be("inner_value");
        gqlError.GqlRawClassification.Should().Be("INNER_CLASSIFICATION");
        gqlError.GqlClassification.Should().Be("CLIENT_ERROR");
    }
}
