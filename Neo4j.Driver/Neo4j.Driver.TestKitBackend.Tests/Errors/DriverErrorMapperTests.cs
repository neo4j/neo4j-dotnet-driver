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

using FluentAssertions;
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

        response.Id.Should().Be("error-1");
        response.ErrorType.Should().Be("ClientError");
        response.Msg.Should().Be("bad cypher");
        response.Code.Should().Be("Neo.ClientError.Statement.SyntaxError");
        response.Retryable.Should().BeFalse();
        response.GqlStatus.Should().Be("50N42");
        response.StatusDescription.Should().Be("some description");
        response.Classification.Should().Be("CLIENT_ERROR");
        response.RawClassification.Should().Be("CLIENT_ERROR_RAW");
        response.DiagnosticRecord!.Should().ContainSingle().Which.Value.Should().Be(diagnosticRecordValue);

        response.Cause.Should().BeOfType<GqlErrorResponse>();
        var cause = (GqlErrorResponse)response.Cause!;
        cause.Msg.Should().Be("cause message");
        cause.GqlStatus.Should().Be("01000");
        cause.StatusDescription.Should().Be("cause description");
        cause.Classification.Should().Be("CAUSE_CLASS");
        cause.RawClassification.Should().Be("CAUSE_RAW");
        cause.DiagnosticRecord!.Should().ContainSingle().Which.Value.Should().Be(causeDiagnosticRecordValue);
        cause.Cause.Should().BeNull();
    }

    [Fact]
    public void Maps_an_argument_exception_to_a_non_retryable_ArgumentError()
    {
        Exception exception = new ArgumentException("encryption and trust cannot both be set");
        _autoMocker.GetMock<IExceptionTypeMapper>().Setup(m => m.Map(exception)).Returns("ArgumentError");

        var registered = new RegistryObject<Exception>("error-1", exception);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(exception)).Returns(registered);

        var mapper = _autoMocker.CreateInstance<DriverErrorMapper>();

        var response = mapper.Map(exception);

        response.Id.Should().Be("error-1");
        response.ErrorType.Should().Be("ArgumentError");
        response.Msg.Should().Be("encryption and trust cannot both be set");
        response.Retryable.Should().BeFalse();
        response.Code.Should().BeNull();
        response.Cause.Should().BeNull();
    }

    [Fact]
    public void Maps_a_time_zone_not_found_exception_to_a_non_retryable_error()
    {
        Exception exception = new TimeZoneNotFoundException("The time zone ID 'Europe/Neo4j' was not found");
        _autoMocker.GetMock<IExceptionTypeMapper>().Setup(m => m.Map(exception)).Returns("TimeZoneNotFoundException");

        var registered = new RegistryObject<Exception>("error-1", exception);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(exception)).Returns(registered);

        var mapper = _autoMocker.CreateInstance<DriverErrorMapper>();

        var response = mapper.Map(exception);

        response.Id.Should().Be("error-1");
        response.ErrorType.Should().Be("TimeZoneNotFoundException");
        response.Msg.Should().Be("The time zone ID 'Europe/Neo4j' was not found");
        response.Retryable.Should().BeFalse();
        response.Code.Should().BeNull();
        response.Cause.Should().BeNull();
    }
}
