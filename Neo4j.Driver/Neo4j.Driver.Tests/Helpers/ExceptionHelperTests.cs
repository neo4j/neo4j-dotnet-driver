// Copyright (c) 2002-2020 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal;
using System;
using System.IO;
using System.Net.Sockets;
using Xunit;

namespace Neo4j.Driver.Tests.Helpers
{
    public class ExceptionHelperTests
    {
        [Theory]
        [MemberData(nameof(ObjectDisposedExceptions))]
        public void ShouldReturnTrue(Exception exc)
        {
            exc.HasCause<ObjectDisposedException>().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ObjectDisposedExceptions))]
        public void ShouldReturnFalse(Exception exc)
        {
            exc.HasCause<SocketException>().Should().BeFalse();
        }

        public static TheoryData<Exception> ObjectDisposedExceptions()
        {
            return new TheoryData<Exception>
            {
                new ObjectDisposedException("socket"),
                new IOException("some message", new ObjectDisposedException("socket")),
                new InvalidOperationException("invalid",
                    new IOException("io", new IOException("io", new ObjectDisposedException("socket")))),
                new AggregateException(new ObjectDisposedException("socket")),
                new AggregateException(new InvalidOperationException(), new ObjectDisposedException("socket")),
                new AggregateException(new IOException("io", new ObjectDisposedException("socket"))),
                new AggregateException(new AggregateException(new ObjectDisposedException("socket"))),
                new AggregateException(new AggregateException(new ObjectDisposedException("socket")),
                    new IOException("io")),
                new AggregateException(new ArgumentException("name"), new InvalidOperationException("io"),
                    new InvalidOperationException("io", new ObjectDisposedException("socket"))),
                new AggregateException(new AggregateException(new IOException("io")),
                    new AggregateException(new InvalidOperationException()),
                    new AggregateException(new InvalidOperationException("io", new ObjectDisposedException("socket")))),
            };
        }
    }
}