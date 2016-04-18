// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class DebugLoggerTests
    {
        internal class StringLogger : BaseOutLogger
        {
            public StringLogger(List<string> lines):base(lines.Add)
            { }
        }

        public class TraceMethod
        {
            [Fact]
            public void ShouldLogByteBufferCorrectly()
            {
                byte[] buffer = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
                var lines = new List<string>();

                var logger = new StringLogger(lines) {Level = LogLevel.Trace};
                logger.Trace("message ", buffer, 0, 10);

                lines.Count.Should().Be(1);
                lines[0].Should().Be("[Trace] => message 01 02 03 04 05 06 07 08 09 0A");
            }

            [Fact]
            public void ShouldThrowExceptionIfBufferOffsetAndCountIsNotSpecified()
            {
                byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                var lines = new List<string>();

                var logger = new StringLogger(lines) { Level = LogLevel.Trace };
                var ex = Record.Exception(() =>logger.Trace("message ", buffer));

                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }
        }

        public class LogMethod
        {
            [Fact]
            public void ShouldLogAllMessages()
            {
                byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                var lines = new List<string>();

                var logger = new StringLogger(lines) { Level = LogLevel.Trace };
                logger.Debug("message ", buffer, "string after buffer", 10);

                lines.Count.Should().Be(1);
                lines[0].Should().Be("[Debug] => message [01 02 03 04 05 06 07 08 09 0A, string after buffer, 10]");
            }
        }
    }
}