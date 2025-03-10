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

using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;
using InvalidOperationException = System.InvalidOperationException;

namespace Neo4j.Driver.Tests.Mapping;

public class DictAsRecordTests
{
    [Fact]
    public void ShouldUsePropertiesOfDict()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.Record.Should().BeSameAs(originalRecord);
        subject.Keys.Should().BeEquivalentTo(dict.Keys);
        subject.Values.Should().BeEquivalentTo(dict);
        subject[0].Should().Be("value1");
        subject[1].Should().Be("value2");
        subject["key1"].Should().Be("value1");
        subject["key2"].Should().Be("value2");
        subject.Get<string>("key1").Should().Be("value1");
        subject.Get<string>("key2").Should().Be("value2");
        subject.GetCaseInsensitive<string>("KEY1").Should().Be("value1");
        subject.GetCaseInsensitive<string>("KEY2").Should().Be("value2");
    }

    [Fact]
    public void ShouldUsePropertiesOfEntity()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var entity = new Node(
            1,
            new[] { "Person" },
            new Dictionary<string, object> { { "key1", "value1" }, { "key2", "value2" } });

        var subject = new DictAsRecord(entity, originalRecord);

        subject.Record.Should().BeSameAs(originalRecord);
        subject.Keys.Should().BeEquivalentTo(entity.Properties.Keys);
        subject.Values.Should().BeEquivalentTo(entity.Properties);
        subject[0].Should().Be("value1");
        subject[1].Should().Be("value2");
        subject["key1"].Should().Be("value1");
        subject["key2"].Should().Be("value2");
        subject.Get<string>("key1").Should().Be("value1");
        subject.Get<string>("key2").Should().Be("value2");
        subject.GetCaseInsensitive<string>("KEY1").Should().Be("value1");
        subject.GetCaseInsensitive<string>("KEY2").Should().Be("value2");
    }

    [Fact]
    public void ShouldThrowIfDictIsNotAnEntityOrDictionary()
    {
        var act = () => new DictAsRecord(42, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryGetShouldReturnFalseIfKeyDoesNotExist()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.TryGet<string>("key3", out var value).Should().BeFalse();
        value.Should().Be(null);
    }

    [Fact]
    public void TryGetShouldReturnFalseIfKeyDoesNotExistCaseInsensitive()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.TryGetCaseInsensitive<string>("KEY3", out var value).Should().BeFalse();
        value.Should().Be(null);
        subject.TryGetValueByCaseInsensitiveKey("KEY3", out var _).Should().BeFalse();
    }

    [Fact]
    public void TryGetShouldReturnTrueIfKeyExists()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.TryGet<string>("key1", out var value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void TryGetShouldReturnTrueIfKeyExistsCaseInsensitive()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.TryGetCaseInsensitive<string>("KEY1", out var value).Should().BeTrue();
        value.Should().Be("value1");
        subject.TryGetValueByCaseInsensitiveKey("KEY1", out var obj).Should().BeTrue();
        obj.Should().Be("value1");
    }

    [Fact]
    public void EnumeratorShouldEnumerateThroughDict()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);
        using var dictEnumerator = dict.GetEnumerator();
        using var enumerator = ((IEnumerable<KeyValuePair<string, object>>)subject).GetEnumerator();
        while (dictEnumerator.MoveNext() && enumerator.MoveNext())
        {
            dictEnumerator.Current.Key.Should().Be(enumerator.Current.Key);
            dictEnumerator.Current.Value.Should().Be(enumerator.Current.Value);
        }
    }

    [Fact]
    public void EnumeratorShouldEnumerateThroughDict_UntypedEnumerable()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);
        using var dictEnumerator = dict.GetEnumerator();
        var enumerator = ((IEnumerable)subject).GetEnumerator();
        while (dictEnumerator.MoveNext() && enumerator.MoveNext())
        {
            dictEnumerator.Current.Key.Should().Be(((KeyValuePair<string, object>)enumerator.Current).Key);
            dictEnumerator.Current.Value.Should().Be(((KeyValuePair<string, object>)enumerator.Current).Value);
        }
    }

    [Fact]
    public void ContainsKeyShouldReturnTrueIfKeyExists()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        ((IReadOnlyDictionary<string, object>)subject).ContainsKey("key1").Should().BeTrue();
    }

    [Fact]
    public void ContainsKeyShouldReturnFalseIfKeyDoesNotExist()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        ((IReadOnlyDictionary<string, object>)subject).ContainsKey("key3").Should().BeFalse();
    }

    [Fact]
    public void IReadOnlyDict_TryGetValueShouldReturnTrueIfKeyExists()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        ((IReadOnlyDictionary<string, object>)subject).TryGetValue("key1", out var value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void IReadOnlyDict_TryGetValueShouldReturnFalseIfKeyDoesNotExist()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        ((IReadOnlyDictionary<string, object>)subject).TryGetValue("KEY3", out var value).Should().BeFalse();
        value.Should().Be(null);
    }

    [Fact]
    public void IReadOnlyDict_CountShouldReturnCorrectCount()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);

        subject.Should().HaveCount(2);
    }

    [Fact]
    public void IReadOnlyDict_KeysShouldReturnCorrectKeys()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);
        ((IReadOnlyDictionary<string, object>)subject).Keys.Should().BeEquivalentTo(dict.Keys);
    }

    // cast to IReadOnlyDictionary<string, object> and check the Values property
    [Fact]
    public void IReadOnlyDict_ValuesShouldReturnCorrectValues()
    {
        var originalRecord = TestRecord.Create(new[] { "name", "age" }, new object[] { "Bob", 42 });
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var subject = new DictAsRecord(dict, originalRecord);
        ((IReadOnlyDictionary<string, object>)subject).Values.Should().BeEquivalentTo(dict.Values);
    }
}
