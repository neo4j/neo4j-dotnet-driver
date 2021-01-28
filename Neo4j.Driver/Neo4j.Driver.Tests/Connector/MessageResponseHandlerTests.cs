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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests.IO;
using Neo4j.Driver.Tests.TestUtil;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;

namespace Neo4j.Driver.Tests
{
    public class MessageResponseHandlerTests
    {
        public class HandleRecordMessageMethod
        {
            [Fact]
            public void NotDequeueFromSentMessagesOrSetsCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResponseCollector.Should().BeNull();
                mrh.HandleRecordMessage(new object[] { "x" });
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResponseCollector.Should().BeNull();
            }

            [Fact]
            public void CallsRecordOnTheCurrentResultBuilder()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();

                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> {{"fields", new List<object> {"x"}}});
                mrh.HandleRecordMessage(new object[] {"x"});

                mockResultBuilder.Verify(x => x.CollectRecord(It.IsAny<object[]>()), Times.Once);
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mockLogger = LoggingHelper.GetTraceEnabledLogger();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> {{"fields", new List<object> {"x"}}});

                mockLogger.ResetCalls();
                mrh.HandleRecordMessage(new object[] {"x"});

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is RecordMessage)), Times.Once);
            }
        }

        public class HandleSuccessMessageMethod
        {
            [Fact]
            public void DequeuesFromSentMessagesAndSetsCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResponseCollector.Should().BeNull();
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mrh.SentMessages.Should().HaveCount(0);
                mrh.CurrentResponseCollector.Should().NotBeNull();
            }

            [Fact]
            public void ShouldCollectFieldsIfMessageContainsFields()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mockResultBuilder.Verify(x=> x.CollectFields(It.IsAny<IDictionary<string, object>>()), Times.Once);
            }

            [Fact]
            public void ShouldCallDoneSuccess()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mockResultBuilder.Verify(x => x.DoneSuccess(), Times.Once);
            }

            [Fact]
            public void ShouldTryToCollectSummaryIfMessageDoesNotContainFields()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "summary", new List<object> { "x" } } });
                mockResultBuilder.Verify(x => x.CollectSummary(It.IsAny<IDictionary<string, object>>()), Times.Once);
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mockLogger = LoggingHelper.GetTraceEnabledLogger();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is SuccessMessage)), Times.Once);
            }

            [Fact]
            public void ShouldSuccessMessageNotClearErrorState()
            {
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll);
                mrh.EnqueueMessage(PullAll);

                mrh.HandleFailureMessage("Neo.ClientError.General.ReadOnly", "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<ClientException>();

                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });

                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().NotBeNull();
            }
        }

        public class HandleIgnoredMessageMethod
        {
            [Fact]
            public void DequeuesFromSentMessagesAndSetsCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResponseCollector.Should().BeNull();
                mrh.HandleIgnoredMessage();
                mrh.SentMessages.Should().HaveCount(0);
                mrh.CurrentResponseCollector.Should().NotBeNull();
            }

            [Fact]
            public void ShouldCallDoneIgnoredIfCurrentResultBuilderNotNull()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();

                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleIgnoredMessage();

                mockResultBuilder.Verify(x => x.DoneIgnored(), Times.Once);
            }

            [Fact]
            public void ShouldNoNPEIfCurrentResultBuilderIsNull()
            {
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll);
                mrh.HandleIgnoredMessage();
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mockLogger = LoggingHelper.GetTraceEnabledLogger();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleIgnoredMessage();

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is IgnoredMessage)), Times.Once);
            }
        }

        public class HandleFailureMessageMethod
        {
            [Theory, MemberData(nameof(ClientErrors))]
            public void ShouldCreateClientExceptionWhenClassificationContainsClientError(string code)
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<ClientException>();
            }

            [Theory, MemberData(nameof(ProtocolErrors))]
            public void ShouldCreateProtocolExceptionWhenClassificationContainsProtocolError(string code)
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<ProtocolException>();
            }

            [Theory, MemberData(nameof(AuthErrors))]
            public void ShouldCreateAuthExceptionWhenClassificationContainsAuthError(string code)
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<AuthenticationException>();
            }

            [Theory, MemberData(nameof(TransientErrors))]
            public void ShouldCreateTransientExceptionWhenClassificationContainsTransientError(string code)
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<TransientException>();
            }

            [Theory, MemberData(nameof(DatabaseErrors))]
            public void ShouldCreateDatabaseExceptionWhenClassificationContainsDatabaseError(string code)
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);

                mrh.HandleFailureMessage(code, "message");
                mrh.HasError.Should().BeTrue();
                mrh.Error.Should().BeOfType<DatabaseException>();
            }

            [Fact]
            public void DequeuesFromSentMessagesAndSetsCurrentBuilder()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mrh = new MessageResponseHandler();

                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.SentMessages.Should().HaveCount(1);
                mrh.CurrentResponseCollector.Should().BeNull();
                mrh.HandleFailureMessage("code.error", "message");
                mrh.SentMessages.Should().HaveCount(0);
                mrh.CurrentResponseCollector.Should().NotBeNull();
            }

            [Fact]
            public void ShouldCallDoneFailureIfCurrentResultBuilderNotNull()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();

                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleFailureMessage("code.error", "message");

                mockResultBuilder.Verify(x => x.DoneFailure(), Times.Once);
            }

            [Fact]
            public void ShouldNoNPEIfCurrentResultBuilderIsNull()
            {
                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(PullAll);
                mrh.HandleFailureMessage("code.error", "message");
            }

            [Fact]
            public void LogsTheMessageToDebug()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();
                var mockLogger = LoggingHelper.GetTraceEnabledLogger();

                var mrh = new MessageResponseHandler(mockLogger.Object);
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleFailureMessage("code.error", "message");

                mockLogger.Verify(x => x.Debug(It.Is<string>(actual => actual.StartsWith("S: ")), It.Is<object[]>(actual => actual.First() is FailureMessage)), Times.Once);
            }

            #region Test Data

            public static IEnumerable<object[]> ProtocolErrors => new[]
            {
                new object[] {"Neo.ClientError.Request.Invalid"},
                new object[] {"Neo.ClientError.Request.InvalidFormat"}
            };

            public static IEnumerable<object[]> AuthErrors => new[]
            {
                new object[] {"Neo.ClientError.Security.Unauthorized"}
            };

            public static IEnumerable<object[]> ClientErrors => new[]
            {
                new object[] {"Neo.ClientError.General.ReadOnly"},
                new object[] {"Neo.ClientError.LegacyIndex.NoSuchIndex"},
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

        public class EnqueueMessageMethod
        {
            [Fact]
            public void ShouldBufferAnyRecordWhenResetIsEnqueue()
            {
                var mockResultBuilder = new Mock<IMessageResponseCollector>();

                var mrh = new MessageResponseHandler();
                mrh.EnqueueMessage(new RunMessage("run something"), mockResultBuilder.Object);
                mrh.EnqueueMessage(PullAll, mockResultBuilder.Object);
                mrh.HandleSuccessMessage(new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
                mrh.HandleRecordMessage(new object[] { "x" });
                mrh.CurrentResponseCollector.Should().NotBeNull();

                mrh.EnqueueMessage(ResetMessage.Reset);
                mrh.CurrentResponseCollector.Should().NotBeNull();

                mrh.HandleRecordMessage(new object[] { "x" });

                mockResultBuilder.Verify(x => x.CollectRecord(It.IsAny<object[]>()), Times.Exactly(2));
                mrh.UnhandledMessageSize.Should().Be(2);
            }
        }
    }
}
