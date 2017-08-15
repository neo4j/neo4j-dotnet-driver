// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class StatementResultCursorExtensionsTests
    {
        public class SingleAsyncMethod
        {
            [Fact]
            public async Task ShouldReturnSingleRecord()
            {
                var mock = new Mock<IStatementResultCursor>();
                var recordMock = new Mock<IRecord>();
                mock.SetupSequence(x => x.FetchAsync()).ReturnsAsync(true).ReturnsAsync(false);
                mock.Setup(x => x.Current).Returns(recordMock.Object);

                var record = await mock.Object.SingleAsync();
                record.Should().Be(recordMock.Object);
            }

            [Fact]
            public async Task ShouldThrowExceptionIfMoreThanOneRecord()
            {
                var mock = new Mock<IStatementResultCursor>();
                var recordMock = new Mock<IRecord>();
                mock.SetupSequence(x => x.FetchAsync()).ReturnsAsync(true).ReturnsAsync(true);
                mock.Setup(x => x.Current).Returns(recordMock.Object);

                var exception = await Record.ExceptionAsync(()=>mock.Object.SingleAsync());
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("more than one");
            }

            [Fact]
            public async Task ShouldThrowExceptionIfNoRecord()
            {
                var mock = new Mock<IStatementResultCursor>();
                var recordMock = new Mock<IRecord>();
                mock.Setup(x => x.FetchAsync()).ReturnsAsync(false);
                mock.Setup(x => x.Current).Returns(recordMock.Object);

                var exception = await Record.ExceptionAsync(() => mock.Object.SingleAsync());
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("empty");
            }

            [Fact]
            public async Task ShouldThrowExceptionIfNullResult()
            {
                IStatementResultCursor result = null;
                var exception = await Record.ExceptionAsync(()=>result.SingleAsync());
                exception.Should().BeOfType<ArgumentNullException>();
            }
        }

        public class ToListAsyncMethod
        {
            [Fact]
            public async Task ShouldReturnEmptyListIfNoRecord()
            {
                var mock = new Mock<IStatementResultCursor>();
                mock.Setup(x => x.FetchAsync()).ReturnsAsync(false);

                var list = await mock.Object.ToListAsync();
                list.Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldReturnList()
            {
                var mock = new Mock<IStatementResultCursor>();
                mock.SetupSequence(x => x.FetchAsync()).
                    ReturnsAsync(true).ReturnsAsync(true).ReturnsAsync(false);
                var record0 = new Mock<IRecord>().Object;
                var record1 = new Mock<IRecord>().Object;
                mock.SetupSequence(x => x.Current).Returns(record0).Returns(record1);

                var list = await mock.Object.ToListAsync();
                list.Count.Should().Be(2);
                list[0].Should().Be(record0);
                list[1].Should().Be(record1);
            }

            [Fact]
            public async Task ShouldThrowExceptionIfNullResult()
            {
                IStatementResultCursor result = null;
                var exception = await Record.ExceptionAsync(() => result.ToListAsync());
                exception.Should().BeOfType<ArgumentNullException>();
            }
        }

        public class ForEachAsyncMethod
        {
            [Fact]
            public async Task ShouldThrowExceptionIfNullResult()
            {
                IStatementResultCursor result = null;
                var exception = await Record.ExceptionAsync(() => result.ForEachAsync(r => { }));
                exception.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public async Task ShouldApplyOnEachElement()
            {
                var mock = new Mock<IStatementResultCursor>();
                mock.SetupSequence(x => x.FetchAsync()).
                    ReturnsAsync(true).ReturnsAsync(true).ReturnsAsync(false);
                var record = new Mock<IRecord>().Object;
                mock.SetupSequence(x => x.Current).Returns(record).Returns(record);

                await mock.Object.ForEachAsync(r => { r.Should().Be(record); });
            }
        }
    }
}
