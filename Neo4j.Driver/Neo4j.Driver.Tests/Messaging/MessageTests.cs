//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Messaging;
using Xunit;
using Xunit.Extensions;

namespace Neo4j.Driver.Tests.Messaging
{
    public class MessageTests
    {
        public class MessageToStringMethod
        {
            [Theory, MemberData("MessageData")]
            public void ShouldPrintTheMessageAsExpected(IMessage message, string expected)
            {
                message.ToString().Should().Be(expected);

            }

            public static IEnumerable<object[]> MessageData => new[]
            {
                new object[] {new FailureMessage("CODE", "MESSAGE"), "FAILURE code=CODE, message=MESSAGE" },
                new object[] {new InitMessage("mydriver"), "INIT `mydriver`"},
                new object[] {new SuccessMessage( new Dictionary<string, object>()), "SUCCESS []" },
                new object[] {new DiscardAllMessage(), "DISCARDALL"},
                new object[] {new IgnoredMessage(), "IGNORED"},
                new object[] {new PullAllMessage(), "PULLALL"},
                new object[] {new RecordMessage( new dynamic[] {1, "a string", new[] {3,4}}),
                    "RECORD [1, a string, [3, 4]]" },
                new object[] {new ResetMessage(), "RESET"},
                new object[] {new RunMessage("A statement", new Dictionary<string, object>
                {
                    { "key1", 1}, { "key2", new[] {2, 4}}
                }), "RUN `A statement` [{key1 : 1}, {key2 : [2, 4]}]"},
            };
        }
    }
}
