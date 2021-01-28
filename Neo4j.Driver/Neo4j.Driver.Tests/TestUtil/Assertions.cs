// Copyright (c) "Neo4j"
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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Numeric;

namespace Neo4j.Driver.Tests
{
    public static class Assertions
    {
        public static AndConstraint<NumericAssertions<T>> BeGreaterOrEqualTo<T>(this NumericAssertions<T> assertion,
            T expected, T accuracy, string because = "", params object[] becauseArgs)
            where T : struct, IConvertible
        {
            var expectedAsDouble = Convert.ToDouble(expected);
            var accuracyAsDouble = Convert.ToDouble(accuracy);

            return assertion.BeGreaterOrEqualTo((T) Convert.ChangeType(expectedAsDouble - accuracyAsDouble, typeof(T)),
                because, becauseArgs);
        }

        public static bool Matches(Action assertion)
        {
            using (new AssertionScope())
            {
                assertion();
            }

            return true;
        }

        public static Func<T, bool> Matches<T>(Action<T> assertion)
        {
            return subject =>
            {
                using (var scope = new AssertionScope())
                {
                    assertion(subject);
                }

                return true;
            };
        }
    }
}