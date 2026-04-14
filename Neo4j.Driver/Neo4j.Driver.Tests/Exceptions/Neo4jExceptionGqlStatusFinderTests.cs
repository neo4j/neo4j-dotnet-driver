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

using System;
using FluentAssertions;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests.Exceptions;

public class Neo4jExceptionGqlStatusFinderTests
{
    private static Neo4jException ExceptionWithStatus(string gqlStatus, FailureMessage cause = null)
    {
        var msg = new FailureMessage { GqlStatus = gqlStatus, GqlCause = cause };
        return Neo4jException.Create(msg);
    }

    public class FindByGqlStatus
    {
        [Fact]
        public void ReturnsThisException_WhenItMatchesDirectly()
        {
            var ex = ExceptionWithStatus("42N04");

            ex.FindByGqlStatus("42N04").Should().BeSameAs(ex);
        }

        [Fact]
        public void ReturnsCause_WhenRootDoesNotMatch()
        {
            var causeMsg = new FailureMessage { GqlStatus = "42N04" };
            var rootMsg = new FailureMessage { GqlStatus = "08N01", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);
            var cause = (Neo4jException)root.InnerException;

            root.FindByGqlStatus("42N04").Should().BeSameAs(cause);
        }

        [Fact]
        public void ReturnsRoot_WhenRootMatchesBeforeCause()
        {
            var causeMsg = new FailureMessage { GqlStatus = "42N04" };
            var rootMsg = new FailureMessage { GqlStatus = "42N04", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);

            root.FindByGqlStatus("42N04").Should().BeSameAs(root);
        }

        [Fact]
        public void Throws_WhenNoMatchInChain()
        {
            var causeMsg = new FailureMessage { GqlStatus = "08N01" };
            var rootMsg = new FailureMessage { GqlStatus = "42N04", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);

            var act = () => root.FindByGqlStatus("99Z99");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*99Z99*");
        }

        [Fact]
        public void Throws_WhenExceptionHasNoGqlStatus()
        {
            var ex = new Neo4jException("code", "message");

            var act = () => ex.FindByGqlStatus("42N04");

            act.Should().Throw<InvalidOperationException>();
        }
    }

    public class TryFindByGqlStatus
    {
        [Fact]
        public void ReturnsTrueAndSetsResult_WhenThisExceptionMatches()
        {
            var ex = ExceptionWithStatus("42N04");

            ex.TryFindByGqlStatus("42N04", out var result).Should().BeTrue();
            result.Should().BeSameAs(ex);
        }

        [Fact]
        public void ReturnsTrueAndSetsCause_WhenRootDoesNotMatch()
        {
            var causeMsg = new FailureMessage { GqlStatus = "42N04" };
            var rootMsg = new FailureMessage { GqlStatus = "08N01", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);
            var cause = (Neo4jException)root.InnerException;

            root.TryFindByGqlStatus("42N04", out var result).Should().BeTrue();
            result.Should().BeSameAs(cause);
        }

        [Fact]
        public void ReturnsFalseAndSetsNull_WhenNoMatchInChain()
        {
            var causeMsg = new FailureMessage { GqlStatus = "08N01" };
            var rootMsg = new FailureMessage { GqlStatus = "42N04", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);

            root.TryFindByGqlStatus("99Z99", out var result).Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void ReturnsFalse_WhenExceptionHasNoGqlStatus()
        {
            var ex = new Neo4jException("code", "message");

            ex.TryFindByGqlStatus("42N04", out var result).Should().BeFalse();
            result.Should().BeNull();
        }
    }

    public class ContainsGqlStatus
    {
        [Fact]
        public void ReturnsTrue_WhenThisExceptionMatches()
        {
            var ex = ExceptionWithStatus("42N04");

            ex.ContainsGqlStatus("42N04").Should().BeTrue();
        }

        [Fact]
        public void ReturnsTrue_WhenCauseMatches()
        {
            var causeMsg = new FailureMessage { GqlStatus = "42N04" };
            var rootMsg = new FailureMessage { GqlStatus = "08N01", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);

            root.ContainsGqlStatus("42N04").Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalse_WhenNoMatchInChain()
        {
            var causeMsg = new FailureMessage { GqlStatus = "08N01" };
            var rootMsg = new FailureMessage { GqlStatus = "42N04", GqlCause = causeMsg };
            var root = Neo4jException.Create(rootMsg);

            root.ContainsGqlStatus("99Z99").Should().BeFalse();
        }
    }
}
