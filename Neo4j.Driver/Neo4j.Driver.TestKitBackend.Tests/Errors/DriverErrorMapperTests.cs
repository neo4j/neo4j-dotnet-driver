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

using Moq.AutoMock;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Errors;

public class DriverErrorMapperTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<DriverErrorMapper>();

    [Fact]
    public void Maps_a_driver_exception_registering_it_and_converting_the_gql_cause_chain()
    {
        var causeFailureMessage = new FailureMessage
        {
            Message = "cause message",
            GqlStatus = "01000",
            GqlStatusDescription = "cause description",
            GqlClassification = "CAUSE_CLASS",
            GqlRawClassification = "CAUSE_RAW",
            GqlDiagnosticRecord = new Dictionary<string, object> { ["OPERATION"] = "" }
        };
        var failureMessage = new FailureMessage("Neo.ClientError.Statement.SyntaxError", "bad cypher")
        {
            GqlStatus = "50N42",
            GqlStatusDescription = "some description",
            GqlClassification = "CLIENT_ERROR",
            GqlRawClassification = "CLIENT_ERROR_RAW",
            GqlDiagnosticRecord = new Dictionary<string, object> { ["CURRENT_SCHEMA"] = "/" },
            GqlCause = causeFailureMessage
        };
        Neo4jException exception = Neo4jException.Create(failureMessage);
        var diagnosticRecordValue = new CypherString("/");
        var causeDiagnosticRecordValue = new CypherString("");
        _autoMocker.GetMock<INativeToCypherMapper>().Setup(m => m.Map("/")).Returns(diagnosticRecordValue);
        _autoMocker.GetMock<INativeToCypherMapper>().Setup(m => m.Map("")).Returns(causeDiagnosticRecordValue);
        _autoMocker.GetMock<IExceptionTypeMapper>().Setup(m => m.Map(exception)).Returns("ClientError");

        var registered = new RegistryObject<Neo4jException>("error-1", exception);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(exception)).Returns(registered);

        var mapper = _autoMocker.CreateInstance<DriverErrorMapper>();

        var response = mapper.Map(exception);

        Assert.Equal("error-1", response.Id);
        Assert.Equal("ClientError", response.ErrorType);
        Assert.Equal("bad cypher", response.Msg);
        Assert.Equal("Neo.ClientError.Statement.SyntaxError", response.Code);
        Assert.False(response.Retryable);
        Assert.Equal("50N42", response.GqlStatus);
        Assert.Equal("some description", response.StatusDescription);
        Assert.Equal("CLIENT_ERROR", response.Classification);
        Assert.Equal("CLIENT_ERROR_RAW", response.RawClassification);
        Assert.Equal(diagnosticRecordValue, Assert.Single(response.DiagnosticRecord!).Value);

        var cause = Assert.IsType<GqlErrorResponse>(response.Cause);
        Assert.Equal("cause message", cause.Msg);
        Assert.Equal("01000", cause.GqlStatus);
        Assert.Equal("cause description", cause.StatusDescription);
        Assert.Equal("CAUSE_CLASS", cause.Classification);
        Assert.Equal("CAUSE_RAW", cause.RawClassification);
        Assert.Equal(causeDiagnosticRecordValue, Assert.Single(cause.DiagnosticRecord!).Value);
        Assert.Null(cause.Cause);
    }
}
