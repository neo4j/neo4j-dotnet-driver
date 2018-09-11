// Copyright (c) 2002-2018 "Neo4j,"
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Logging
{
    public class LegacyLoggerLoggingAdapterTests
    {
        [Fact]
        public void ShouldReturnTheSameLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logging = new LegacyLoggerLoggingAdapter(legacyLogger.Object);
            var logger = logging.GetLogger("name one");
            logger.Should().Be(logging.GetLogger("name two"));
            logger.Should().Be(logging.GetLogger("name three"));
        }

        [Fact]
        public void ShouldDelegateFatalToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            var message = "a message";
            var error = new Exception("an exception");
            logger.Fatal(error, message);

            legacyLogger.Verify(x=>x.Error(message, error));
        }

        [Fact]
        public void ShouldDelegateFatalMessageToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            logger.Fatal("message: {0}", "hello world");
            legacyLogger.Verify(x=>x.Error("message: hello world", null));
        }

        [Fact]
        public void ShouldDelegateErrorToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            var message = "a message";
            var error = new Exception("an exception");
            logger.Error(error, message);

            legacyLogger.Verify(x=>x.Error(message, error));
        }

        [Fact]
        public void ShouldDelegateErrorMessageToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            logger.Error("message: {0}", "hello world");
            legacyLogger.Verify(x=>x.Error("message: hello world", null));
        }

        [Fact]
        public void ShouldDelegateWarnToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            var message = "a message";
            var error = new Exception("an exception");
            logger.Warn(error, message);

            legacyLogger.Verify(x=>x.Info(message, error));
        }

        [Fact]
        public void ShouldDelegateWarnMessageToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            logger.Warn("message: {0}", "hello world");
            legacyLogger.Verify(x=>x.Info("message: hello world"));
        }

        [Fact]
        public void ShouldDelegateInfoToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            var message = "a message {0}";
            var error = new Exception("an exception");
            logger.Info(message, error);

            legacyLogger.Verify(x=>x.Info("a message System.Exception: an exception"));
        }

        [Fact]
        public void ShouldDelegateInfoMessageToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            logger.Info("message: {0}", "hello world");
            legacyLogger.Verify(x=>x.Info("message: hello world"));
        }

        [Fact]
        public void ShouldDelegateTraceToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            var message = "a message {0}";
            var error = new Exception("an exception");
            logger.Trace(message, error);

            legacyLogger.Verify(x=>x.Trace("a message System.Exception: an exception"));
        }

        [Fact]
        public void ShouldDelegateTraceMessageToLegacyLogger()
        {
            var legacyLogger = new Mock<ILogger>();
            var logger = new LegacyLoggerDriverLoggerAdapter(legacyLogger.Object);

            logger.Trace("message: {0}", "hello world");
            legacyLogger.Verify(x=>x.Trace("message: hello world"));
        }

        [Fact]
        public void ShouldAllowNullLegacyLogger()
        {
            var logger = new LegacyLoggerDriverLoggerAdapter(null);

            // should not throw any error
            logger.Fatal("1");
            logger.Error("2");
            logger.Warn("3");
            logger.Debug("4");
            logger.Trace("5");
        }
    }
}