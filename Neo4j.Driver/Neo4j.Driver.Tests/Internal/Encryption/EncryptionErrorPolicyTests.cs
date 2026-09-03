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

#nullable enable

using System;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptionErrorPolicyTests
{
    private static EncryptionErrorPolicy CreateSubject() => new();

    [Fact]
    public void Throw_Neo4jException_RethrowsTheSameInstance()
    {
        var driverError = new TransientException("Neo.TransientError.General.X", "retry me");

        var act = () => CreateSubject().Throw("encryption", driverError);

        var thrown = act.Should().Throw<TransientException>();
        thrown.Which.Should().BeSameAs(driverError);
    }

    [Fact]
    public void Throw_NonDriverException_ThrowsPropertyEncryptionExceptionWithCause()
    {
        var cause = new InvalidOperationException("kes blew up");

        var act = () => CreateSubject().Throw("key creation", cause);

        var thrown = act.Should().Throw<PropertyEncryptionException>();
        thrown.Which.InnerException.Should().BeSameAs(cause);
        thrown.Which.Message.Should().Contain("key creation");
    }
}
