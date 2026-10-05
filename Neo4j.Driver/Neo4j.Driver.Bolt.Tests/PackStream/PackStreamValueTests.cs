// // Copyright (c) "Neo4j"
// // Neo4j Sweden AB [https://neo4j.com]
// // 
// // Licensed under the Apache License, Version 2.0 (the "License").
// // You may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// // 
// //     http://www.apache.org/licenses/LICENSE-2.0
// // 
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
//
// using System.Buffers;
// using FluentAssertions;
// using Neo4j.Driver.Bolt.PackStream;
// using NUnit.Framework;
//
// namespace Neo4j.Driver.Bolt.Tests.PackStream;
//
// public class PackStreamValueTests
// {
//     // ByteValue tests - tiny ints encoded in marker byte itself
//     [TestCase(new byte[] { 0x00 }, (sbyte)0)]           // Zero
//     [TestCase(new byte[] { 0x01 }, (sbyte)1)]           // Positive
//     [TestCase(new byte[] { 0x7F }, (sbyte)127)]         // Max positive tiny int
//     [TestCase(new byte[] { 0xF0 }, (sbyte)-16)]         // Min negative tiny int
//     [TestCase(new byte[] { 0xFF }, (sbyte)-1)]          // Negative
//     public void CorrectlyDecodesByteValue(byte[] inputBytes, sbyte expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.TinyIntValue;
//
//         result.Should().Be(expected);
//     }
//
//     // Int8Value tests - 0xC8 marker
//     [TestCase(new byte[] { 0xC8, 0x00 }, (sbyte)0)]           // Zero
//     [TestCase(new byte[] { 0xC8, 0x7F }, (sbyte)127)]         // Max positive
//     [TestCase(new byte[] { 0xC8, 0x80 }, (sbyte)-128)]        // Min negative
//     [TestCase(new byte[] { 0xC8, 0xFF }, (sbyte)-1)]          // -1
//     [TestCase(new byte[] { 0xC8, 0x01 }, (sbyte)1)]           // Small positive
//     public void CorrectlyDecodesInt8Value(byte[] inputBytes, sbyte expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.Int8Value;
//
//         result.Should().Be(expected);
//     }
//
//     // Int16Value tests - 0xC9 marker, big-endian
//     [TestCase(new byte[] { 0xC9, 0x00, 0x00 }, (short)0)]           // Zero
//     [TestCase(new byte[] { 0xC9, 0x00, 0x01 }, (short)1)]           // Small positive
//     [TestCase(new byte[] { 0xC9, 0x01, 0x00 }, (short)256)]         // 256
//     [TestCase(new byte[] { 0xC9, 0x7F, 0xFF }, (short)32767)]       // Max positive
//     [TestCase(new byte[] { 0xC9, 0x80, 0x00 }, (short)-32768)]      // Min negative
//     [TestCase(new byte[] { 0xC9, 0xFF, 0xFF }, (short)-1)]          // -1
//     public void CorrectlyDecodesInt16Value(byte[] inputBytes, short expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.Int16Value;
//
//         result.Should().Be(expected);
//     }
//
//     // Int32Value tests - 0xCA marker, big-endian
//     [TestCase(new byte[] { 0xCA, 0x00, 0x00, 0x00, 0x00 }, 0)]                  // Zero
//     [TestCase(new byte[] { 0xCA, 0x00, 0x00, 0x00, 0x01 }, 1)]                  // Small positive
//     [TestCase(new byte[] { 0xCA, 0x00, 0x01, 0x00, 0x00 }, 65536)]              // 65536
//     [TestCase(new byte[] { 0xCA, 0x7F, 0xFF, 0xFF, 0xFF }, int.MaxValue)]       // Max positive
//     [TestCase(new byte[] { 0xCA, 0x80, 0x00, 0x00, 0x00 }, int.MinValue)]       // Min negative
//     [TestCase(new byte[] { 0xCA, 0xFF, 0xFF, 0xFF, 0xFF }, -1)]                 // -1
//     public void CorrectlyDecodesInt32Value(byte[] inputBytes, int expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.Int32Value;
//
//         result.Should().Be(expected);
//     }
//
//     // LongValue tests - 0xCB marker, big-endian
//     [TestCase(new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0L)]              // Zero
//     [TestCase(new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, 1L)]              // Small positive
//     [TestCase(new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 }, 4294967296L)]     // > int.MaxValue
//     [TestCase(new byte[] { 0xCB, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, long.MaxValue)]   // Max positive
//     [TestCase(new byte[] { 0xCB, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, long.MinValue)]   // Min negative
//     [TestCase(new byte[] { 0xCB, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, -1L)]             // -1
//     public void CorrectlyDecodesLongValue(byte[] inputBytes, long expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.LongValue;
//
//         result.Should().Be(expected);
//     }
//
//     // FloatValue tests - 0xC6 marker (Float32), big-endian IEEE 754
//     [TestCase(new byte[] { 0xC6, 0x00, 0x00, 0x00, 0x00 }, 0.0f)]               // Zero
//     [TestCase(new byte[] { 0xC6, 0x3F, 0x80, 0x00, 0x00 }, 1.0f)]               // 1.0
//     [TestCase(new byte[] { 0xC6, 0xBF, 0x80, 0x00, 0x00 }, -1.0f)]              // -1.0
//     [TestCase(new byte[] { 0xC6, 0x40, 0x48, 0xF5, 0xC3 }, 3.14f)]              // Pi approx
//     [TestCase(new byte[] { 0xC6, 0x7F, 0x7F, 0xFF, 0xFF }, float.MaxValue)]     // Max
//     [TestCase(new byte[] { 0xC6, 0xFF, 0x7F, 0xFF, 0xFF }, float.MinValue)]     // Min
//     public void CorrectlyDecodesFloatValue(byte[] inputBytes, float expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.FloatValue;
//
//         result.Should().BeApproximately(expected, 0.001f);
//     }
//
//     // DoubleValue tests - 0xC1 marker (Float64), big-endian IEEE 754
//     [TestCase(new byte[] { 0xC1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0.0)]                     // Zero
//     [TestCase(new byte[] { 0xC1, 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1.0)]                     // 1.0
//     [TestCase(new byte[] { 0xC1, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, -1.0)]                    // -1.0
//     [TestCase(new byte[] { 0xC1, 0x40, 0x09, 0x21, 0xFB, 0x54, 0x44, 0x2D, 0x18 }, 3.141592653589793)]       // Pi
//     [TestCase(new byte[] { 0xC1, 0x7F, 0xEF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, double.MaxValue)]         // Max
//     [TestCase(new byte[] { 0xC1, 0xFF, 0xEF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, double.MinValue)]         // Min
//     public void CorrectlyDecodesDoubleValue(byte[] inputBytes, double expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.DoubleValue;
//
//         result.Should().BeApproximately(expected, 0.0000001);
//     }
//
//     // BooleanValue tests
//     [TestCase(new byte[] { 0xC3 }, true)]    // True
//     [TestCase(new byte[] { 0xC2 }, false)]   // False
//     public void CorrectlyDecodesBooleanValue(byte[] inputBytes, bool expected)
//     {
//         var bytes = new ReadOnlySequence<byte>(inputBytes);
//         var value = PackStreamValue.Read(bytes);
//
//         var result = value.BooleanValue;
//
//         result.Should().Be(expected);
//     }
//
//     // [Test]
//     // public void ZeroSizeSlice()
//     // {
//     //     var seq = new ReadOnlySequence<byte>([0xC0]);
//     //     PackStreamValue.Read(seq).Size.Should().Be(1); // just the marker byte
//     // }
// }
