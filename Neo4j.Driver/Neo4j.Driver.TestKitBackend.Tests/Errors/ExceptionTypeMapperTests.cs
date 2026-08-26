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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.TestKitBackend.Errors;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Errors;

public class ExceptionTypeMapperTests
{
    private readonly ExceptionTypeMapper _mapper = new();

    [Fact]
    public void Maps_ClientException_to_ClientError()
    {
        _mapper.Map(new ClientException("boom")).Should().Be("ClientError");
    }

    [Fact]
    public void Maps_TransientException_to_DriverError()
    {
        _mapper.Map(new TransientException("code", "boom")).Should().Be("DriverError");
    }

    [Fact]
    public void Maps_the_base_Neo4jException_to_Neo4jError()
    {
        _mapper.Map(new Neo4jException("boom")).Should().Be("Neo4jError");
    }

    [Fact]
    public void Maps_AuthorizationException_to_AuthorizationExpired()
    {
        _mapper.Map(new AuthorizationException("boom")).Should().Be("AuthorizationExpired");
    }

    [Fact]
    public void Maps_TokenExpiredException_to_ClientError()
    {
        _mapper.Map(new TokenExpiredException("boom")).Should().Be("ClientError");
    }

    [Fact]
    public void Maps_AuthenticationException_to_AuthenticationError()
    {
        _mapper.Map(new AuthenticationException("boom")).Should().Be("AuthenticationError");
    }

    [Fact]
    public void Maps_UnknownSecurityException_to_OtherSecurityException()
    {
        _mapper.Map(new UnknownSecurityException("boom")).Should().Be("OtherSecurityException");
    }

    [Fact]
    public void Falls_back_to_the_type_name_with_an_Error_suffix_for_an_unmapped_exception()
    {
        _mapper.Map(new FormatException("boom")).Should().Be("FormatError");
    }

    [Fact]
    public void Maps_InvalidOperationException_to_itself_unchanged()
    {
        _mapper.Map(new InvalidOperationException("boom")).Should().Be("InvalidOperationException");
    }

    [Fact]
    public void Maps_ArgumentException_to_ArgumentError()
    {
        _mapper.Map(new ArgumentException("boom")).Should().Be("ArgumentError");
    }

    [Fact]
    public void Maps_ReauthException_to_UnsupportedFeatureException()
    {
        _mapper.Map(new ReauthException(false)).Should().Be("UnsupportedFeatureException");
    }

    [Fact]
    public void Maps_TransactionTerminatedException_to_TransactionTerminatedError()
    {
        _mapper.Map(new TransactionTerminatedException(new Exception("boom"))).Should().Be("TransactionTerminatedError");
    }

    [Fact]
    public void Maps_ResultConsumedException_to_ResultConsumedError()
    {
        _mapper.Map(new ResultConsumedException("boom")).Should().Be("ResultConsumedError");
    }

    [Fact]
    public void Maps_ConnectionReadTimeoutException_to_ConnectionReadTimeoutError()
    {
        _mapper.Map(new ConnectionReadTimeoutException("boom")).Should().Be("ConnectionReadTimeoutError");
    }

    [Fact]
    public void Maps_ServiceUnavailableException_to_ServiceUnavailableError()
    {
        _mapper.Map(new ServiceUnavailableException("boom")).Should().Be("ServiceUnavailableError");
    }

    public static IEnumerable<object[]> LegacyDivergentMappings()
    {
        yield return [new TimeoutException("boom"), "DriverError"];
        yield return [new TransactionClosedException("boom"), "ClientError"];
        yield return [new TransactionNestingException("boom"), "TransactionNestingException"];
        yield return [new NotSupportedException("boom"), "NotSupportedException"];
        yield return [new StatementArgumentException("boom"), "ArgumentError"];
        yield return [new UnsupportedFeatureException("boom"), "UnsupportedFeatureException"];
        yield return [new ObjectDisposedException("boom"), "ObjectDisposedException"];
        yield return [new ArgumentNullException("boom"), "ArgumentError"];
        yield return [new ArgumentOutOfRangeException("boom"), "ArgumentError"];
    }

    [Theory]
    [MemberData(nameof(LegacyDivergentMappings))]
    public void Matches_the_legacy_errorType_name_for_a_divergent_exception_type(Exception exception, string expected)
    {
        _mapper.Map(exception).Should().Be(expected);
    }
}
