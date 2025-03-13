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

using System;
using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Neo4j.Driver.Tests.TestUtil;
using static Neo4j.Driver.Tests.TestUtil.Assertions;

namespace Neo4j.Driver.Tests.Reactive.Utils;

public static class Utils
{
    public static object Record(string[] keys, params object[] fields)
    {
        if (keys.Length != fields.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(keys),
                $"{nameof(keys)} and {nameof(fields)} should be of same size.");
        }

        if (keys.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keys), $"{nameof(keys)} should contain at least 1 item.");
        }

        return TestRecord.Create(keys, fields);
    }

    public static Func<string[], bool> MatchesKeys(params string[] keys)
    {
        return r => Matches(() => r.Should().BeEquivalentTo(keys));
    }

    public static Func<IRecord, bool> MatchesRecord(string[] keys, params object[] fields)
    {
        return Matches<IRecord>(
            rec =>
            {
                rec.Keys.Should().BeEquivalentTo(keys);
                rec.Values.Values.Should().BeEquivalentTo(fields);
            });
    }

    public static Func<IResultSummary, bool> MatchesSummary(
        object sample,
        Func<EquivalencyAssertionOptions<object>, EquivalencyAssertionOptions<object>> options = null)
    {
        return s => Matches(() => s.Should().BeEquivalentTo(sample, options ?? (o => o)));
    }

    public static Func<Exception, bool> MatchesException<TExpected>()
        where TExpected : Exception
    {
        return MatchesException<TExpected>(exc => true);
    }

    public static Func<Exception, bool> MatchesException<TExpected>(Expression<Func<TExpected, bool>> predicate)
        where TExpected : Exception
    {
        return e => Matches(() => e.Should().BeAssignableTo<TExpected>().Which.Should().Match(predicate));
    }
}
