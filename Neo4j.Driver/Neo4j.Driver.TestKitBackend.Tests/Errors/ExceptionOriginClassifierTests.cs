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
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver.Internal.Temporal;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Errors;

public class ExceptionOriginClassifierTests
{
    private readonly ExceptionOriginClassifier _classifier = new();

    [Fact]
    public void A_real_exception_thrown_directly_by_driver_code_originates_in_the_driver()
    {
        var exception = Record.Exception(() => Config.Builder.WithFetchSize(-5));

        exception.Should().BeOfType<ArgumentOutOfRangeException>();
        _classifier.OriginatesInDriver(exception!).Should().BeTrue();
    }

    [Fact]
    public void A_BCL_exception_thrown_deep_inside_driver_called_code_still_originates_in_the_driver()
    {
        var exception = Record.Exception(() => TimeZoneMapping.Get("Not/A/Real/Zone"));

        exception.Should().BeOfType<TimeZoneNotFoundException>();
        _classifier.OriginatesInDriver(exception!).Should().BeTrue();
    }

    [Fact]
    public void A_real_exception_thrown_by_the_backends_own_code_does_not_originate_in_the_driver()
    {
        var exception = Record.Exception(() => new ObjectStore(NullLogger.Instance).Get<object>("missing"));

        exception.Should().BeOfType<TestKitProtocolException>();
        _classifier.OriginatesInDriver(exception!).Should().BeFalse();
    }

    [Fact]
    public void A_Neo4jException_thrown_by_the_backends_own_code_still_originates_in_the_driver()
    {
        var exception = new ClientException("simulating a driver-defined failure from a user collaborator");

        _classifier.OriginatesInDriver(exception).Should().BeTrue();
    }
}
