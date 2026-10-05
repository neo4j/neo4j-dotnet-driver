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

using FluentAssertions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.TestHelpers.Tests;

public class ByteArrayBuilderTests
{
    [Test]
    public void CreatesByteArray()
    {
        var zeroes = new ByteArrayBuilder().Zeroes(10);
        zeroes.Should().BeEquivalentTo([0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
    }
    
    [Test]
    public void ConcatenatesFluently()
    {
        var bytes = new ByteArrayBuilder()
            .Range(0, 5)
            .Range(10, 5);
        
        bytes.Should().BeEquivalentTo([0, 1, 2, 3, 4, 10, 11, 12, 13, 14]);
    }

    [Test]
    public void BuildsPackStreamMessage()
    {
        var messages = new ByteArrayBuilder()
            .PackStreamMessage([0x01, 0x02, 0x03, 0x04]);
        
        messages.Should().BeEquivalentTo(new byte[] {0x00, 0x04, 0x01, 0x02, 0x03, 0x04});
    }
    
    [Test]
    public void BuildsMultiplePackStreamMessages()
    {
        var messages = new ByteArrayBuilder()
            .PackStreamMessage([1, 2, 3, 4, 5, 6, 7, 8])
            .PackStreamMessage([12, 13, 14, 15, 16, 17])
            .PackStreamMessage([])
            .PackStreamMessage([0, 0, 0, 0, 0, 0, 0, 0, 0]);
        
        messages.Should().BeEquivalentTo(new byte[]
        {
            0, 8, 1, 2, 3, 4, 5, 6, 7, 8,
            0, 6, 12, 13, 14, 15, 16, 17,
            0, 0,
            0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0
        });
    }
}
