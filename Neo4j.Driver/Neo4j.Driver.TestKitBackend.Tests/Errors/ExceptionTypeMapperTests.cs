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
    public void Falls_back_to_the_exact_type_name_for_an_unmapped_exception()
    {
        _mapper.Map(new InvalidOperationException("boom")).Should().Be("InvalidOperationException");
    }
}
