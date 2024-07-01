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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests;

public class ResultCursorExtensionsTests
{
    public class SingleAsyncMethod
    {
        [Fact]
        public async Task ShouldReturnSingleRecord()
        {
            var mockRecord = new Mock<IRecord>();
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(mockRecord.Object)
                .Returns(default(IRecord));

            var mockCursor = new Mock<IResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var record = await mockCursor.Object.SingleAsync();
            record.Should().BeSameAs(mockRecord.Object);
        }

        [Fact]
        public async Task ShouldThrowExceptionIfMoreThanOneRecord()
        {
            var mockRecord = new Mock<IRecord>();
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(mockRecord.Object)
                .Returns(mockRecord.Object)
                .Returns(default(IRecord));

            var mockCursor = new Mock<IResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var exception = await Record.ExceptionAsync(() => mockCursor.Object.SingleAsync());

            exception.Should()
                .BeOfType<InvalidOperationException>()
                .Which.Message.Should()
                .Contain("more than one");
        }

        [Fact]
        public async Task ShouldThrowExceptionIfNoRecord()
        {
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(default(IRecord));

            var mockCursor = new Mock<IResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var exception = await Record.ExceptionAsync(() => mockCursor.Object.SingleAsync());
            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Contain("empty");
        }

        [Fact]
        public async Task ShouldThrowExceptionIfNullResult()
        {
            IResultCursor result = null;
            var exception = await Record.ExceptionAsync(() => result.SingleAsync());
            exception.Should().BeOfType<ArgumentNullException>();
        }
    }

    public class ToListAsyncMethod
    {
        [Fact]
        public async Task ShouldReturnEmptyListIfNoRecord()
        {
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(default(IRecord));

            var mockCursor = new Mock<IResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var list = await mockCursor.Object.ToListAsync();
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldReturnList()
        {
            var record0 = new Mock<IRecord>().Object;
            var record1 = new Mock<IRecord>().Object;
            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(record0)
                .Returns(record1)
                .Returns(default(IRecord));

            var mockCursor = new Mock<IResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var list = await mockCursor.Object.ToListAsync();
            list.Count.Should().Be(2);
            list[0].Should().BeSameAs(record0);
            list[1].Should().BeSameAs(record1);
        }

        [Fact]
        public async Task ShouldThrowExceptionIfNullResult()
        {
            IResultCursor result = null;
            var exception = await Record.ExceptionAsync(() => result.ToListAsync());
            exception.Should().BeOfType<ArgumentNullException>();
        }
    }

    public class ForEachAsyncMethod
    {
        [Fact]
        public async Task ShouldThrowExceptionIfNullResult()
        {
            IResultCursor result = null;
            var exception = await Record.ExceptionAsync(() => result.ForEachAsync(_ => {}));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldApplyOnEachElement()
        {
            var mockRecord = new Mock<IRecord>();
            var mockRecord2 = new Mock<IRecord>();

            var enumerator = new Mock<IAsyncEnumerator<IRecord>>();
            enumerator
                .SetupSequence(x => x.MoveNextAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            enumerator
                .SetupSequence(x => x.Current)
                .Returns(mockRecord.Object)
                .Returns(mockRecord2.Object);

            var mockCursor = new Mock<IInternalResultCursor>();
            mockCursor
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(enumerator.Object);

            var index = 0;
            await mockCursor.Object.ForEachAsync(
                r =>
                {
                    index++;
                    r.Should().BeSameAs(index == 1 ? mockRecord.Object : mockRecord2.Object);
                });
        }
    }
}
