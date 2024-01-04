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
using System.Threading.Tasks;
using Neo4j.Driver.Tests.TestBackend.Exceptions;
using Neo4j.Driver.Tests.TestBackend.Protocol.JsonConverters;
using Neo4j.Driver.Tests.TestBackend.Transaction;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Session;

internal class SessionWriteTransaction : ProtocolObject
{
    public SessionWriteTransactionType data { get; set; } = new();

    [JsonIgnore] public string TransactionId { get; set; }

    public override async Task Process(Controller controller)
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

        await sessionContainer.Session.ExecuteWriteAsync(
            async tx =>
            {
                sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNothing);

                TransactionId = controller.TransactionManager.AddTransaction(
                    new TransactionWrapper(
                        tx as IAsyncTransaction,
                        async cursor =>
                        {
                            var result = ProtocolObjectFactory.CreateObject<Result.Result>();
                            await result.PopulateRecords(cursor).ConfigureAwait(false);
                            return result.uniqueId;
                        }));

                sessionContainer.SessionTransactions.Add(TransactionId);

                await controller.SendResponse(new ProtocolResponse("RetryableTry", TransactionId).Encode())
                    .ConfigureAwait(false);

                await controller.Process(
                    false,
                    e =>
                    {
                        switch (sessionContainer.RetryState)
                        {
                            case NewSession.SessionState.RetryAbleNothing:
                                return true;

                            case NewSession.SessionState.RetryAblePositive:
                                return false;

                            case NewSession.SessionState.RetryAbleNegative:
                                throw e;

                            default:
                                return true;
                        }
                    });
            },
            data.TransactionConfig);
    }

    public override string Respond()
    {
        var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

        if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNothing)
        {
            throw new ArgumentException("Should never hit this code with a RetryAbleNothing");
        }

        if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNegative)
        {
            if (string.IsNullOrEmpty(sessionContainer.RetryableErrorId))
            {
                return ExceptionManager
                    .GenerateExceptionResponse(new TestKitClientException("Error from client in retryable tx"))
                    .Encode();
            }

            var exception = ((ProtocolException)ObjManager.GetObject(sessionContainer.RetryableErrorId)).ExceptionObj;
            return ExceptionManager.GenerateExceptionResponse(exception).Encode();
        }

        return new ProtocolResponse("RetryableDone", new {}).Encode();
    }

    [JsonConverter(typeof(BaseSessionTypeJsonConverter<SessionWriteTransactionType>))]
    public class SessionWriteTransactionType : BaseSessionType
    {
    }
}
