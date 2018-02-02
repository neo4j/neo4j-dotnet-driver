// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class BoltWriterTests
    {

        public class WithFlush
        {

            [Fact]
            public void WritesInitMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new InitMessage("a", new Dictionary<string, object>()));
                writer.Flush();

                mocks.VerifyResult("00 05 B2 01 81 61 A0 00 00");
            }

            [Fact]
            public void WritesRunMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new RunMessage("RETURN 1 AS num"));
                writer.Flush();

                mocks.VerifyResult("00 13 b2 10 8f 52 45 54 55 52 4e 20 31 20 41 53 20 6e 75 6d a0 00 00");
            }

            [Theory]
            [InlineData(1, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 01 00 00")]
            [InlineData(long.MinValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 80 00 00 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 7F FF FF FF FF FF FF FF 00 00")]
            [InlineData((long) int.MinValue - 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB FF FF FF FF 7F FF FF FF 00 00")]
            [InlineData((long) int.MaxValue + 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 00 00 00 00 80 00 00 00 00 00")]
            [InlineData(int.MinValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 80 00 00 00 00 00")]
            [InlineData(int.MaxValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 7F FF FF FF 00 00")]
            [InlineData(short.MinValue - 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA FF FF 7F FF 00 00")]
            [InlineData(short.MaxValue + 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 00 00 80 00 00 00")]
            [InlineData(short.MinValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 80 00 00 00")]
            [InlineData(short.MaxValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 7F FF 00 00")]
            [InlineData(-129, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 FF 7F 00 00")]
            [InlineData(128, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 00 80 00 00")]
            [InlineData(-128, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 80 00 00")]
            [InlineData(-17, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 EF 00 00")]
            [InlineData(-16, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 F0 00 00")]
            [InlineData(127, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 7F 00 00")]
            public void WritesRunMessageWithIntegerParamCorrectly(long value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"integer", value}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(true, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C3 00 00")]
            [InlineData(false, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C2 00 00")]
            public void WritesRunMessageWithBoolParamCorrectly(bool value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", value}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(1.00, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(double.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 7F EF FF FF FF FF FF FF 00 00")]
            [InlineData(double.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 FF EF FF FF FF FF FF FF 00 00")]
            public void WritesRunMessageWithDoubleParamCorrectly(double value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", value}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(1.0f, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(float.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 47 EF FF FF E0 00 00 00 00 00")]
            [InlineData(float.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 C7 EF FF FF E0 00 00 00 00 00")]
            public void WritesRunMessageWithFloatParamCorrectly(float value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", value}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData("它们的语言学归属在西方语言学界存在争议",
                "00 45 B2 10 80 A1 85 76 61 6C 75 65 D0 39 E5 AE 83 E4 BB AC E7 9A 84 E8 AF AD E8 A8 80 E5 AD A6 E5 BD 92 E5 B1 9E E5 9C A8 E8 A5 BF E6 96 B9 E8 AF AD E8 A8 80 E5 AD A6 E7 95 8C E5 AD 98 E5 9C A8 E4 BA 89 E8 AE AE 00 00")]
            [InlineData("", "00 0B B2 10 80 A1 85 76 61 6C 75 65 80 00 00")]
            [InlineData("kåkåkå kå",
                "00 18 B2 10 80 A1 85 76 61 6C 75 65 8D 6B C3 A5 6B C3 A5 6B C3 A5 20 6B C3 A5 00 00")]
            public void WritesRunMessageWithStringParamCorrectly(string value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", value}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(expectedBytes);
            }

            [Fact]
            public void WritesRunMessageWithArrayListParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", new ArrayList()}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult("00 0B B2 10 80 A1 85 76 61 6C 75 65 90 00 00");
            }

            [Fact]
            public void WritesRunMessageWithArrayParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", new[] {1, 2}}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult("00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00");
            }

            [Fact]
            public void WritesRunMessageWithGenericListParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> {{"value", new List<int> {1, 2}}};
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult("00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00");
            }

            [Fact]
            public void WritesRunMessageWithDictionaryParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object>
                {
                    {"value", new Dictionary<string, object> {{"key1", 1}, {"key2", 2}}}
                };
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult("00 17 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 02 00 00");
            }

            [Fact]
            public void WritesRunMessageWithDictionaryMixedTypesParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object>
                {
                    {"value", new Dictionary<string, object> {{"key1", 1}, {"key2", "a string value"}}}
                };
                writer.Write(new RunMessage("", values));
                writer.Flush();

                mocks.VerifyResult(
                    "00 25 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 8E 61 20 73 74 72 69 6E 67 20 76 61 6C 75 65 00 00");
            }

            [Fact]
            public void WritesPullAllMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new PullAllMessage());
                writer.Flush();

                mocks.VerifyResult("00 02 B0 3F 00 00");
            }

            [Fact]
            public void WritesDiscardAllMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new DiscardAllMessage());
                writer.Flush();

                mocks.VerifyResult("00 02 B0 2F 00 00");
            }

            [Fact]
            public void WritesResetMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new ResetMessage());
                writer.Flush();

                mocks.VerifyResult("00 02 B0 0F 00 00");
            }

            [Fact]
            public void WritesAckFailureMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new AckFailureMessage());
                writer.Flush();

                mocks.VerifyResult("00 02 B0 0E 00 00");
            }

        }

        public class WithFlushAsync
        {

            [Fact]
            public async void WritesInitMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new InitMessage("a", new Dictionary<string, object>()));
                await writer.FlushAsync();

                mocks.VerifyResult("00 05 B1 01 81 61 A0 00 00");
            }

            [Fact]
            public async void WritesRunMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new RunMessage("RETURN 1 AS num"));
                await writer.FlushAsync();

                mocks.VerifyResult("00 13 b2 10 8f 52 45 54 55 52 4e 20 31 20 41 53 20 6e 75 6d a0 00 00");
            }

            [Theory]
            [InlineData(1, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 01 00 00")]
            [InlineData(long.MinValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 80 00 00 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 7F FF FF FF FF FF FF FF 00 00")]
            [InlineData((long)int.MinValue - 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB FF FF FF FF 7F FF FF FF 00 00")]
            [InlineData((long)int.MaxValue + 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 00 00 00 00 80 00 00 00 00 00")]
            [InlineData(int.MinValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 80 00 00 00 00 00")]
            [InlineData(int.MaxValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 7F FF FF FF 00 00")]
            [InlineData(short.MinValue - 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA FF FF 7F FF 00 00")]
            [InlineData(short.MaxValue + 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 00 00 80 00 00 00")]
            [InlineData(short.MinValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 80 00 00 00")]
            [InlineData(short.MaxValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 7F FF 00 00")]
            [InlineData(-129, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 FF 7F 00 00")]
            [InlineData(128, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 00 80 00 00")]
            [InlineData(-128, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 80 00 00")]
            [InlineData(-17, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 EF 00 00")]
            [InlineData(-16, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 F0 00 00")]
            [InlineData(127, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 7F 00 00")]
            public async void WritesRunMessageWithIntegerParamCorrectly(long value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "integer", value } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(true, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C3 00 00")]
            [InlineData(false, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C2 00 00")]
            public async void WritesRunMessageWithBoolParamCorrectly(bool value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", value } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(1.00, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(double.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 7F EF FF FF FF FF FF FF 00 00")]
            [InlineData(double.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 FF EF FF FF FF FF FF FF 00 00")]
            public async void WritesRunMessageWithDoubleParamCorrectly(double value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", value } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData(1.0f, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(float.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 47 EF FF FF E0 00 00 00 00 00")]
            [InlineData(float.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 C7 EF FF FF E0 00 00 00 00 00")]
            public async void WritesRunMessageWithFloatParamCorrectly(float value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", value } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(expectedBytes);
            }

            [Theory]
            [InlineData("它们的语言学归属在西方语言学界存在争议",
                "00 45 B2 10 80 A1 85 76 61 6C 75 65 D0 39 E5 AE 83 E4 BB AC E7 9A 84 E8 AF AD E8 A8 80 E5 AD A6 E5 BD 92 E5 B1 9E E5 9C A8 E8 A5 BF E6 96 B9 E8 AF AD E8 A8 80 E5 AD A6 E7 95 8C E5 AD 98 E5 9C A8 E4 BA 89 E8 AE AE 00 00")]
            [InlineData("", "00 0B B2 10 80 A1 85 76 61 6C 75 65 80 00 00")]
            [InlineData("kåkåkå kå",
                "00 18 B2 10 80 A1 85 76 61 6C 75 65 8D 6B C3 A5 6B C3 A5 6B C3 A5 20 6B C3 A5 00 00")]
            public async void WritesRunMessageWithStringParamCorrectly(string value, string expectedBytes)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", value } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(expectedBytes);
            }

            [Fact]
            public async void WritesRunMessageWithArrayListParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", new ArrayList() } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult("00 0B B2 10 80 A1 85 76 61 6C 75 65 90 00 00");
            }

            [Fact]
            public async void WritesRunMessageWithArrayParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", new[] { 1, 2 } } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult("00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00");
            }

            [Fact]
            public async void WritesRunMessageWithGenericListParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object> { { "value", new List<int> { 1, 2 } } };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult("00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00");
            }

            [Fact]
            public async void WritesRunMessageWithDictionaryParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object>
                {
                    {"value", new Dictionary<string, object> {{"key1", 1}, {"key2", 2}}}
                };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult("00 17 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 02 00 00");
            }

            [Fact]
            public async void WritesRunMessageWithDictionaryMixedTypesParamCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                var values = new Dictionary<string, object>
                {
                    {"value", new Dictionary<string, object> {{"key1", 1}, {"key2", "a string value"}}}
                };
                writer.Write(new RunMessage("", values));
                await writer.FlushAsync();

                mocks.VerifyResult(
                    "00 25 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 8E 61 20 73 74 72 69 6E 67 20 76 61 6C 75 65 00 00");
            }

            [Fact]
            public async void WritesPullAllMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new PullAllMessage());
                await writer.FlushAsync();

                mocks.VerifyResult("00 02 B0 3F 00 00");
            }

            [Fact]
            public async void WritesDiscardAllMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new DiscardAllMessage());
                await writer.FlushAsync();

                mocks.VerifyResult("00 02 B0 2F 00 00");
            }

            [Fact]
            public async void WritesResetMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new ResetMessage());
                await writer.FlushAsync();

                mocks.VerifyResult("00 02 B0 0F 00 00");
            }

            [Fact]
            public async void WritesAckFailureMessageCorrectly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new BoltWriter(mocks.OutputStream);

                writer.Write(new AckFailureMessage());
                await writer.FlushAsync();

                mocks.VerifyResult("00 02 B0 0E 00 00");
            }

        }
    }

}
