using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Neo4j.Driver.Tests
{
    class ResultCreator
    {
        public static ResultCursor CreateResult(int keySize, int recordSize=1)
        {
            var records = new List<Record>(recordSize);

            var keys = new List<string>(keySize);
            for (int i = 0; i < keySize; i++)
            {
                keys.Add($"str{i}");
            }

            for (int j = 0; j < recordSize; j++)
            {
                var values = new List<dynamic>();
                for (int i = 0; i < keySize; i++)
                {
                    values.Add(i);
                }
                records.Add(new Record(keys.ToArray(), values.ToArray()));
            }
            
            return new ResultCursor(records, keys.ToArray());
        }
    }
    public class ResultCursorTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldThrowArgumentNullExceptionIfRecordsIsNull()
            {
                var ex = Xunit.Record.Exception(() => new ResultCursor((IEnumerable<Record>)null, new string[] {"test"}));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullExceptionIfKeysIsNull()
            {
                var ex = Xunit.Record.Exception(() => new ResultCursor(new List<Record>(), null));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldSetKeysProperlyIfKeysNotNull()
            {
                var result = new ResultCursor(new List<Record>(), new string[] {"test"});
                result.Keys.Should().HaveCount(1);
                result.Keys.Should().Contain("test");
            }

            [Fact]
            public void ShouldGetEnumeratorFromRecords()
            {
                Mock<IEnumerable<Record>> mock = new Mock<IEnumerable<Record>>();
                var cursor = new ResultCursor(mock.Object, new string[] {"test"});

                mock.Verify(x => x.GetEnumerator(), Times.Once);
            }
        }

        public class CloseMethod
        {
            [Fact]
            public void ShouldSetOpenToFalse()
            {
                var cursor = ResultCreator.CreateResult(1);
                cursor.Close();
                cursor.IsOpen().Should().BeFalse();
            }

            [Fact]
            public void ShouldCallDiscardOnEnumberator()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();

                var cursor = new ResultCursor(mock.Object, new string[] { "test" });
                cursor.Close();
                mock.Verify(x => x.Discard(), Times.Once);
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionWhenCallingCloseMultipleTimes()
            {
              
                var cursor = ResultCreator.CreateResult(1);

                cursor.Close();
                var ex = Xunit.Record.Exception(() => cursor.Close());
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        public class NextMethod
        {
            [Fact]
            public void ShouldThrowExceptionIfCursorIsClosed()
            {
                var cursor = ResultCreator.CreateResult(1);

                cursor.Close();
                var ex = Xunit.Record.Exception(() => cursor.Next());
                ex.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public void ShouldReturnTrueAndMoveCursorToNext()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();
                mock.Setup(x => x.HasNext()).Returns(true);
                var cursor = new ResultCursor(mock.Object, new string[] { "test" });

                cursor.Next().Should().BeTrue();
                mock.Verify(x => x.HasNext(), Times.Once);
                mock.Verify(x => x.Next(), Times.Once);
                cursor.Position().Should().Be(0);
            }

            [Fact]
            public void ShouldReturnFalseAndNotMoveCursorIfLast()
            {
                Mock<IPeekingEnumerator<Record>> mock = new Mock<IPeekingEnumerator<Record>>();
                mock.Setup(x => x.HasNext()).Returns(false);
                var cursor = new ResultCursor(mock.Object, new string[] { "test" });

                cursor.Next().Should().BeFalse();
                mock.Verify(x => x.HasNext(), Times.Once);
                mock.Verify(x => x.Next(), Times.Never);
                cursor.Position().Should().Be(-1);
            }

            [Fact(Skip = "Pending API Review")]
            public void ShouldDiscardIfLimitReached()
            {
                throw new NotImplementedException();
            }
        }

        public class RecordMethod
        {
            [Fact]
            public void ShouldReturnRecordIfHasRecord()
            {
                var cursor = ResultCreator.CreateResult(1);
                cursor.Next();
                cursor.Record().Should().NotBeNull();
            }

            [Fact]
            public void ShouldThrowInvalidOperationExceptionIfHasNoRecord()
            {
                var cursor = ResultCreator.CreateResult(1);
                var ex = Xunit.Record.Exception(() => cursor.Record());
                ex.Should().BeOfType<InvalidOperationException>();
            }
        }

        public class RecordsMethod
        {
            [Fact]
            public void ShouldReturnRecords()
            {
                var cursor = ResultCreator.CreateResult(2,2);
                var records = cursor.Stream().ToList();
                records.Count.Should().Be(2);
                Assert.Equal(0, records[0].Values["str0"]);
                Assert.Equal(1, records[1].Values["str1"]);
            }
        }
    }
}
