// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling;

public class MessageFomatTests
{
    public class ZoneDateTimeTests
    {
        static readonly byte[] NonUtcEncoderBytes = { (byte)'F', (byte)'f' };
        static readonly byte[] UtcEncoderBytes = { (byte)'I', (byte)'i' };

        [Theory]
        [InlineData(3, 0)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(4, 3)]
        [InlineData(4, 4)]
        public void MessageFormatShouldDefaultToNonUtcDateSerializers(int major, int minor)
        {
            var format = new MessageFormat(new BoltProtocolVersion(major, minor));
            format.ReaderStructHandlers.Keys.Should()
                .Contain(NonUtcEncoderBytes)
                .And.NotContain(UtcEncoderBytes);

            format.ReaderStructHandlers
                .Where(x => NonUtcEncoderBytes.Contains(x.Key))
                .Select(x => x.Value)
                .Distinct()
                .First()
                .Should()
                .Be(ZonedDateTimeSerializer.Instance);
        }

        [Theory]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        [InlineData(6, 0)]
        public void MessageFormatShouldDefaultToUtcDateSerializers(int major, int minor)
        {
            var format = new MessageFormat(new BoltProtocolVersion(major, minor));
            format.ReaderStructHandlers.Keys.Should()
                .Contain(UtcEncoderBytes)
                .And.NotContain(NonUtcEncoderBytes);

            format.ReaderStructHandlers
                .Where(x => UtcEncoderBytes.Contains(x.Key))
                .Select(x => x.Value)
                .Distinct()
                .First()
                .Should()
                .Be(UtcZonedDateTimeSerializer.Instance);
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        public void MessageFormatShouldIgnoreUseUtcEncoderWhenInvalid(int major, int minor)
        {
            var format = new MessageFormat(new BoltProtocolVersion(major, minor));

            //ignored for < 4.3
            format.UseUtcEncoder();

            format.ReaderStructHandlers.Keys.Should()
                .Contain(NonUtcEncoderBytes)
                .And.NotContain(UtcEncoderBytes);

            format.ReaderStructHandlers
                .Where(x => NonUtcEncoderBytes.Contains(x.Key))
                .Select(x => x.Value)
                .Distinct()
                .First()
                .Should()
                .Be(ZonedDateTimeSerializer.Instance);
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        [InlineData(6, 0)]
        public void MessageFormatShouldUseUtcEncoderWhenInvalid(int major, int minor)
        {
            var format = new MessageFormat(new BoltProtocolVersion(major, minor));

            //ignored for version > 4.4 and applied for 4.3 & 4.4
            format.UseUtcEncoder();

            format.ReaderStructHandlers.Keys.Should()
                .Contain(UtcEncoderBytes)
                .And.NotContain(NonUtcEncoderBytes);

            format.ReaderStructHandlers
                .Where(x => UtcEncoderBytes.Contains(x.Key))
                .Select(x => x.Value)
                .Distinct()
                .First()
                .Should()
                .Be(UtcZonedDateTimeSerializer.Instance);
        }
    }
}
