using Xunit;
using System.IO;
using Neo4j.Driver.Tests.TestBackend;
using FluentAssertions;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class ReaderWriterUnitTests
    {
        [Fact]
        public async void ReaderTest()
        {
            const string testString = "Test string 123";
            using (MemoryStream memStream = new MemoryStream(100))
            {
                using (StreamWriter streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(testString);
                    streamWriter.Flush();
                    memStream.Seek(0, SeekOrigin.Begin);
                    

                    Reader testReader = new Reader(memStream);
                    var result = await testReader.Read().ConfigureAwait(false);

                    result.Should().Be(testString);
                }
            }
        }

        [Fact]
        public async void WriterTest()
        {
            const string testString = "Test string 123";
            using (MemoryStream memStream = new MemoryStream(100))
            {
                using (StreamReader streamReader = new StreamReader(memStream))
                {
                    Writer testWriter = new Writer(memStream);
                    await testWriter.WriteAsync(testString).ConfigureAwait(false);
                    memStream.Seek(0, SeekOrigin.Begin);

                    var result = streamReader.ReadLine();

                    result.Should().Be(testString);
                }
            }
        }
    }



}
