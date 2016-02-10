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
// 

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class MessageResponseHandlerTests
    {
        public class HandleRecordMessageMethod
        {
            [Fact]
            public void CallsRecordOnTheCurrentResultBuilder()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();

                var mrh = new MessageResponseHandler();
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> {{"fields", new List<object> {"x"}}});
                mrh.HandleRecordMessage(new dynamic[] {"x"});

                mockResultBuilder.Verify(x => x.Record(It.IsAny<dynamic[]>()), Times.Once);
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mockLogger = new Mock<ILogger>();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> {{"fields", new List<object> {"x"}}});

                mockLogger.ResetCalls();
                mrh.HandleRecordMessage(new dynamic[] {"x"});

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is RecordMessage)), Times.Once);
            }
        }

        public class HandleSuccessMessageMethod
        {
            [Fact]
            public void DequeuesFromSentMessagesAndSetsCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();

                // Two messages are queued, as one will be popped off when handling a success message.
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResultBuilder.Should().BeNull();
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mrh.SentMessages.Should().HaveCount(0);
                mrh.CurrentResultBuilder.Should().NotBeNull();
            }

            [Fact]
            public void ShouldCollectFieldsIfMessageContainsFields()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();

                // Two messages are queued, as one will be popped off when handling a success message.
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mockResultBuilder.Verify(x=> x.CollectFields(It.IsAny<IDictionary<string, object>>()), Times.Once);
            }

            [Fact]
            public void ShouldTryToCollectSummaryMetaIfMessageDoesNotContainFields()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();

                // Two messages are queued, as one will be popped off when handling a success message.
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "summary", new List<object> { "x" } } });
                mockResultBuilder.Verify(x => x.CollectSummaryMeta(It.IsAny<IDictionary<string, object>>()), Times.Once);
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mockLogger = new Mock<ILogger>();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is SuccessMessage)), Times.Once);
            }
        }

        public class ClearMethod
        {
            [Fact]
            public void ShouldClearResultBuildersSendMessagesAndTheCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();
                
                // Two messages are queued, as one will be popped off when handling a success message.
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);
                // We need to handle the success message to et the CurrentResultBuilder
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });

                mrh.SentMessages.Should().HaveCount(1);
                mrh.ResultBuilders.Should().HaveCount(1);
                mrh.CurrentResultBuilder.Should().NotBeNull();

                mrh.Clear();

                mrh.SentMessages.Should().HaveCount(0);
                mrh.ResultBuilders.Should().HaveCount(0);
                mrh.CurrentResultBuilder.Should().BeNull();
            }
        }

        public class HandleFailureMessageMethod
        {
            [Theory, MemberData("ClientErrors")]
            public void ShouldCreateClientExceptionWhenClassificationContainsClientError(string code)
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<ClientException>();
            }

            [Theory, MemberData("TransientErrors")]
            public void ShouldCreateTransientExceptionWhenClassificationContainsTransientError(string code)
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<TransientException>();
            }

            [Theory, MemberData("DatabaseErrors")]
            public void ShouldCreateDatabaseExceptionWhenClassificationContainsDatabaseError(string code)
            {
                var mockResultBuilder = new Mock<IResultBuilder>();
                var mrh = new MessageResponseHandler();
                mrh.Register(new PullAllMessage(), mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<DatabaseException>();
            }

            #region Test Data
            public static IEnumerable<object[]> ClientErrors => new[]
            {
                new object[] {"Neo.ClientError.General.ReadOnly"},
                new object[] {"Neo.ClientError.LegacyIndex.NoSuchIndex"},
                new object[] {"Neo.ClientError.Request.Invalid"},
                new object[] {"Neo.ClientError.Request.InvalidFormat"},
                new object[] {"Neo.ClientError.Schema.ConstraintAlreadyExists"},
                new object[] {"Neo.ClientError.Schema.ConstraintVerificationFailure"},
                new object[] {"Neo.ClientError.Schema.ConstraintViolation"},
                new object[] {"Neo.ClientError.Schema.IllegalTokenName"},
                new object[] {"Neo.ClientError.Schema.IndexAlreadyExists"},
                new object[] {"Neo.ClientError.Schema.IndexBelongsToConstraint"},
                new object[] {"Neo.ClientError.Schema.IndexLimitReached"},
                new object[] {"Neo.ClientError.Schema.LabelLimitReached"},
                new object[] {"Neo.ClientError.Schema.NoSuchConstraint"},
                new object[] {"Neo.ClientError.Schema.NoSuchIndex"},
                new object[] {"Neo.ClientError.Security.AuthenticationFailed"},
                new object[] {"Neo.ClientError.Security.AuthenticationRateLimit"},
                new object[] {"Neo.ClientError.Security.AuthorizationFailed"},
                new object[] {"Neo.ClientError.Statement.ArithmeticError"},
                new object[] {"Neo.ClientError.Statement.ConstraintViolation"},
                new object[] {"Neo.ClientError.Statement.EntityNotFound"},
                new object[] {"Neo.ClientError.Statement.InvalidArguments"},
                new object[] {"Neo.ClientError.Statement.InvalidSemantics"},
                new object[] {"Neo.ClientError.Statement.InvalidSyntax"},
                new object[] {"Neo.ClientError.Statement.InvalidType"},
                new object[] {"Neo.ClientError.Statement.NoSuchLabel"},
                new object[] {"Neo.ClientError.Statement.NoSuchProperty"},
                new object[] {"Neo.ClientError.Statement.ParameterMissing"},
                new object[] {"Neo.ClientError.Transaction.ConcurrentRequest"},
                new object[] {"Neo.ClientError.Transaction.EventHandlerThrewException"},
                new object[] {"Neo.ClientError.Transaction.HookFailed"},
                new object[] {"Neo.ClientError.Transaction.InvalidType"},
                new object[] {"Neo.ClientError.Transaction.MarkedAsFailed"},
                new object[] {"Neo.ClientError.Transaction.UnknownId"},
                new object[] {"Neo.ClientError.Transaction.ValidationFailed"}
            };

            public static IEnumerable<object[]> TransientErrors => new[]
            {
                new object[] {"Neo.TransientError.General.DatabaseUnavailable"},
                new object[] {"Neo.TransientError.Network.UnknownFailure"},
                new object[] {"Neo.TransientError.Schema.ModifiedConcurrently"},
                new object[] {"Neo.TransientError.Security.ModifiedConcurrently"},
                new object[] {"Neo.TransientError.Statement.ExternalResourceFailure"},
                new object[] {"Neo.TransientError.Transaction.AcquireLockTimeout"},
                new object[] {"Neo.TransientError.Transaction.ConstraintsChanged"},
                new object[] {"Neo.TransientError.Transaction.DeadlockDetected"}
            };

            public static IEnumerable<object[]> DatabaseErrors => new[]
            {
                new object[] {"Neo.DatabaseError.General.CorruptSchemaRule"},
                new object[] {"Neo.DatabaseError.General.FailedIndex"},
                new object[] {"Neo.DatabaseError.General.UnknownFailure"},
                new object[] {"Neo.DatabaseError.Schema.ConstraintCreationFailure"},
                new object[] {"Neo.DatabaseError.Schema.ConstraintDropFailure"},
                new object[] {"Neo.DatabaseError.Schema.DuplicateSchemaRule"},
                new object[] {"Neo.DatabaseError.Schema.IndexCreationFailure"},
                new object[] {"Neo.DatabaseError.Schema.IndexDropFailure"},
                new object[] {"Neo.DatabaseError.Schema.NoSuchLabel"},
                new object[] {"Neo.DatabaseError.Schema.NoSuchPropertyKey"},
                new object[] {"Neo.DatabaseError.Schema.NoSuchRelationshipType"},
                new object[] {"Neo.DatabaseError.Schema.NoSuchSchemaRule"},
                new object[] {"Neo.DatabaseError.Statement.ExecutionFailure"},
                new object[] {"Neo.DatabaseError.Transaction.CouldNotBegin"},
                new object[] {"Neo.DatabaseError.Transaction.CouldNotCommit"},
                new object[] {"Neo.DatabaseError.Transaction.CouldNotRollback"},
                new object[] {"Neo.DatabaseError.Transaction.CouldNotWriteToLog"},
                new object[] {"Neo.DatabaseError.Transaction.ReleaseLocksFailed"}
            };

            #endregion Test Data
        }
    }
}