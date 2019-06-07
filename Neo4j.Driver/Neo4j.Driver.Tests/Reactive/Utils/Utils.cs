// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.Reactive
{
    public static class Utils
    {
        public static object Record(string[] keys, params object[] fields)
        {
            if (keys.Length != fields.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(keys),
                    $"{nameof(keys)} and {nameof(fields)} should be of same size.");
            }

            if (keys.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keys), $"{nameof(keys)} should contain at least 1 item.");
            }

            return new
            {
                Keys = keys,
                Values = Enumerable.Range(0, keys.Length)
                    .Select(i => new KeyValuePair<string, object>(keys[i], fields[i])).ToDictionary(),
            };
        }

        public static Func<string[], bool> MatchesKeys(params string[] keys)
        {
            return r => Matches(() => r.Should().BeEquivalentTo(keys));
        }

        public static Func<IRecord, bool> MatchesRecord(string[] keys, params object[] fields)
        {
            return r => Matches(() => r.Should().BeEquivalentTo(Record(keys, fields)));
        }

        public static Func<IResultSummary, bool> MatchesSummary(object sample,
            Func<EquivalencyAssertionOptions<object>, EquivalencyAssertionOptions<object>> options = null)
        {
            return s => Matches(() => s.Should().BeEquivalentTo<object>(sample, options ?? (o => o)));
        }

        public static Func<Exception, bool> MatchesException<TExpected>(Expression<Func<TExpected, bool>> predicate)
            where TExpected : Exception
        {
            return e => Matches(() => e.Should().BeAssignableTo<TExpected>().Which.Should().Match(predicate));
        }
    }
}