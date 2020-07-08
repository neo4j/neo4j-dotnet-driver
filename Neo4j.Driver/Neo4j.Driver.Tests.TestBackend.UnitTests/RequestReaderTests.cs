using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.IO;
using Neo4j.Driver.Tests.TestBackend;
using Moq;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class StreamParserTests
    {
        [Theory]
        [InlineData(@"#request begin
                      {
                        ""name"": ""NewDriver"",
                        ""data"": {
                            ""uri"": ""127.0.0.1"",
                            ""authorizationToken"" : {
                                ""name"":null,
                                ""data"": {
                                    ""scheme"":null, 
                                    ""principal"":null, 
                                    ""credentials"":null, 
                                    ""realm"":null, 
                                    ""ticket"":null
                                }
                            }   
                        }
                      }
                      #request end",
                     @"{""name"": ""NewDriver"",""data"": {""uri"": ""127.0.0.1"",""authorizationToken"" : {""name"":null,""data"": {""scheme"":null,""principal"":null,""credentials"":null,""realm"":null,""ticket"":null}}}}")]
        public async Task ShouldProcessNext(string testString, string resultString)
        {
            using (MemoryStream memStream = new MemoryStream(100))
            {
                using (StreamWriter streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(testString);
                    streamWriter.Flush();
                    memStream.Seek(0, SeekOrigin.Begin);

                    var reader = new Reader(memStream);
                    ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
                    RequestReader parser = new RequestReader(reader);

                    await parser.ParseNextRequest();

                    parser.CurrentObjectData.Should().Be(resultString);
                }
            }
        }

        [Theory]       
        [InlineData(@"{
                        ""name"": ""NewDriver"",
                        ""data"": {
                            ""uri"": ""127.0.0.1"",
                            ""authorizationToken"": {
                                ""name"":null,
                                ""data"": {
                                    ""scheme"":null, 
                                    ""principal"":null, 
                                    ""credentials"":null, 
                                    ""realm"":null, 
                                    ""ticket"":null
                                }
                            }
                        }
                       }")]
        [InlineData(@"{
                        ""name"": ""NewSession"",
                        ""data"": {
                            ""driverId"": ""127.0.0.1"",
                            ""accessMode"" : ""mode"",
                            ""bookmarks"" : ""123""
                          }
                       }")]
        [InlineData(@"{
                        ""name"": ""AuthorizationToken"",
                        ""data"": {
                            ""scheme"": ""thescheme"",
                            ""principal"" : ""thepricipal"",
                            ""credentials"" : ""userAccount"",
                            ""realm"": ""empire"",
                            ""ticket"": ""toRide""
                          }
                       }")]
        [InlineData(@"{
                        ""name"": ""SessionRun"",
                        ""data"": {
                            ""sessionId"": ""NewSession01"",
                            ""cypher"" : ""CREATE (n:TestLabel {num: 1, txt: 'abc'}) RETURN n"",
                            ""params"": {
                                ""context"": ""123"",
                                ""user"": ""neo4j"",
                                ""password"": ""1234""
                            }
                        }
                       }")]
        public void ShouldProcessTestsObjectNoThrow(string jsonString)
        {
            var moqMemoryStream = new Mock<MemoryStream>();
            moqMemoryStream.Setup(x => x.CanRead).Returns(true);
            var moqReader = new Mock<Reader>(moqMemoryStream.Object);
            ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
            var requestReader = new RequestReader(moqReader.Object);

            requestReader.CurrentObjectData = jsonString;
            var createdObject = requestReader.CreateObjectFromData();            
        }
    }
}
