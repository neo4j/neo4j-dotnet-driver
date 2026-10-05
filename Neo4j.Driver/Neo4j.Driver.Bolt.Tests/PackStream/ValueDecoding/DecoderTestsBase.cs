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

using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

internal abstract class DecoderTestsBase<T> : UnitTestBase<T> where T : ValueDecoderBase
{
    [Test]
    public void ShouldCorrectlyIdentifyHandledMarkers()
    {
        for (var i = 0; i <= 255; i++)
        {
            var b = (byte)i;
            var isMarkerByteHandled = Subject.IsMarkerByteHandled(b);
            var isHandledMsg = $"IsMarkerByteHandled(0x{b:X2}) returns {isMarkerByteHandled}";
            
            var isByteInHandledArray = Subject.HandledMarkerBytes.Contains(b);
            var arrayMsg = $"HandledMarkerBytes.Contains(0x{b:X2}) returns {isByteInHandledArray}";

            if (isMarkerByteHandled != isByteInHandledArray)
            {
                Assert.Fail($"{isHandledMsg} but {arrayMsg}");
            }
        }
    }
}
