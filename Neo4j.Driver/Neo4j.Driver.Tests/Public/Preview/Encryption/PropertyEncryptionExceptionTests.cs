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
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Public.Preview.Encryption;

public class PropertyEncryptionExceptionTests
{
    [Fact]
    public void CarriesTheMessageAndCause()
    {
        var cause = new InvalidOperationException("boom");

        var subject = new PropertyEncryptionException("encryption failed", cause);

        subject.Message.Should().Be("encryption failed");
        subject.InnerException.Should().BeSameAs(cause);
    }

    [Fact]
    public void HasTheUnknownGqlStatus()
    {
        var subject = new PropertyEncryptionException("encryption failed");

        subject.GqlStatus.Should().Be("50N42");
    }

    [Fact]
    public void HasTheDefaultGqlDiagnosticRecord()
    {
        var subject = new PropertyEncryptionException("encryption failed");

        subject.GqlDiagnosticRecord.Should().Contain("OPERATION", "");
        subject.GqlDiagnosticRecord.Should().Contain("OPERATION_CODE", "0");
        subject.GqlDiagnosticRecord.Should().Contain("CURRENT_SCHEMA", "/");
    }
}
