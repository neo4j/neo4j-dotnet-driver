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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.PropertyEncryption;

public class FixedIvProviderTests
{
    private static readonly byte[] Iv = Enumerable.Range(0, 12).Select(i => (byte)i).ToArray();

    private readonly FixedIvProvider _provider =
        AutoMocker.ForTesting<FixedIvProvider>().CreateInstance<FixedIvProvider>();

    [Fact]
    public void Draws_a_random_iv_when_none_was_set()
    {
        var first = _provider.GetIv();
        var second = _provider.GetIv();

        first.Should().HaveCount(12);
        second.Should().HaveCount(12);
        second.Should().NotEqual(first);
    }

    [Fact]
    public void Returns_the_iv_that_was_set()
    {
        _provider.SetNextIv(Iv);

        var actualIv = _provider.GetIv();
        actualIv.Should().Equal(Iv);
    }

    [Fact]
    public void Consumes_the_iv_so_the_next_draw_is_random()
    {
        _provider.SetNextIv(Iv);

        _ = _provider.GetIv();
        var secondIv = _provider.GetIv();

        secondIv.Should().NotEqual(Iv);
    }

    [Fact]
    public void Rejects_an_iv_that_is_not_twelve_bytes()
    {
        var act = () => _provider.SetNextIv([1, 2, 3]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Replaces_an_iv_that_is_still_pending()
    {
        var replacementIv = Enumerable.Range(100, 12).Select(i => (byte)i).ToArray();
        _provider.SetNextIv(Iv);
        _provider.SetNextIv(replacementIv);

        var actualIv = _provider.GetIv();

        actualIv.Should().Equal(replacementIv);
    }

}
