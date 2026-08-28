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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class LocalKeyEncapsulationServiceTests
{
    private const int DekLength = 32;
    private const int IvLength = 12;

    private static readonly byte[] Kek = Sequence(32, seed: 0x50);

    private static readonly byte[] Dek = Sequence(DekLength);
    private static readonly byte[] Iv = Sequence(IvLength);

    private readonly AutoMocker _autoMock = new(MockBehavior.Loose);

    private record NoOptions : IKeyEncapsulationOptions
    {
        public IReadOnlyDictionary<string, string> ToMap()
        {
            return new Dictionary<string, string>();
        }
    }

    private LocalKeyEncapsulationService CreateSubject()
    {
        return new LocalKeyEncapsulationService(
            Kek,
            _autoMock.GetMock<IAeadCipher>().Object,
            new SequentialRandom(),
            _autoMock.GetMock<IBase64Codec>().Object);
    }

    [Fact]
    public async Task Encapsulate_WrapsTheGeneratedDekUnderTheKekAndReturnsTheEncapsulation()
    {
        var wrapped = new CipherResult([0xAA, 0xBB, ..Sequence(16, seed: 0xC0)], TagLength: 16);
        _autoMock.GetMock<IAeadCipher>()
            .Setup(c => c.Encrypt(Kek, Matches(Iv), Matches(Dek), Matches(Array.Empty<byte>())))
            .Returns(wrapped);
        _autoMock.GetMock<IBase64Codec>()
            .Setup(b => b.Encode(Matches(Iv)))
            .Returns("encoded-iv");

        var result = await CreateSubject().EncapsulateAsync(new NoOptions());

        result.Key.Should().Equal(Dek);
        result.Encapsulation.Should().Equal(wrapped.CipherOutput);
        result.Options.ToMap()["iv"].Should().Be("encoded-iv");
    }

    [Fact]
    public async Task Decapsulate_DecodesTheStoredIvAndUnwrapsUnderTheKek()
    {
        var encapsulation = new byte[] { 1, 2, 3, 4 };
        var dek = Sequence(DekLength, seed: 0x77);
        _autoMock.GetMock<IBase64Codec>().Setup(b => b.Decode("stored-iv")).Returns(Iv);
        _autoMock.GetMock<IAeadCipher>()
            .Setup(c => c.Decrypt(Kek, Matches(Iv), encapsulation, Matches(Array.Empty<byte>())))
            .Returns(dek);

        var options = new Dictionary<string, string> { ["iv"] = "stored-iv" };

        var result = await CreateSubject().DecapsulateAsync(encapsulation, options);

        result.Should().Equal(dek);
    }
}
