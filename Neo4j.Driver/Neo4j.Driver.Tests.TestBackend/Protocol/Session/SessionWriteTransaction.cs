// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SessionWriteTransaction : ProtocolObject
{
    public SessionWriteTransactionType data { get; set; } = new();
    [JsonIgnore] public string TransactionId { get; set; }

    public override async Task ProcessAsync(Controller controller)
    {
        var sessionContainer = ObjManager.GetObject<NewSession>(data.sessionId);

        await sessionContainer.Session.ExecuteWriteAsync(async tx =>
        {
            sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNothing);

            TransactionId = controller.TransactionManager.AddTransaction(
                new TransactionWrapper<IAsyncTransaction>(tx as IAsyncTransaction,
                cursor =>
                {
                    var result = ProtocolObjectFactory.CreateObject<Result>();
                    result.ResultCursor = cursor;
                    return Task.FromResult(result.UniqueId);
                }));

            sessionContainer.SessionTransactions.Add(TransactionId);

            await controller.SendResponseAsync(new ProtocolResponse("RetryableTry", TransactionId).Encode())
                ;

            await controller.ProcessAsync(false, e =>
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
        }, TransactionConfig);
    }

    public override Task ReactiveProcessAsync(Controller controller)
    {
        var sessionContainer = ObjManager.GetObject<NewSession>(data.sessionId);

        return sessionContainer.RxSession.ExecuteWrite(tx =>
        {
            sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNothing);

            TransactionId = controller.ReactiveTransactionManager.AddTransaction(
                new TransactionWrapper<IRxTransaction>(tx as IRxTransaction,
                    cursor =>
                    {
                        var result = ProtocolObjectFactory.CreateObject<Result>();
                        result.ResultCursor = cursor;
                        return Task.FromResult(result.UniqueId);
                    }));

            sessionContainer.SessionTransactions.Add(TransactionId);

            controller
                .SendResponseAsync(new ProtocolResponse("RetryableTry", TransactionId).Encode())
                .GetAwaiter().GetResult();

            controller.ProcessAsync(false, e =>
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
            }).GetAwaiter().GetResult();
            return Observable.Empty<Unit>();
        }, TransactionConfig).ToTask();
    }

    public override string Respond()
    {
        var sessionContainer = ObjManager.GetObject<NewSession>(data.sessionId);

        if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNothing)
            throw new ArgumentException("Should never hit this code with a RetryAbleNothing");

        if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNegative)
        {
            if (string.IsNullOrEmpty(sessionContainer.RetryableErrorId))
                return ExceptionManager
                    .GenerateExceptionResponse(new TestKitClientException("Error from client in retryable tx"))
                    .Encode();

            var exception = ObjManager.GetObject<ProtocolExceptionWrapper>(sessionContainer.RetryableErrorId)
                .ExceptionObj;
            return ExceptionManager.GenerateExceptionResponse(exception).Encode();
        }

        return new ProtocolResponse("RetryableDone", new { }).Encode();
    }

    private void TransactionConfig(TransactionConfigBuilder configBuilder)
    {
        if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);

        try
        {
            if (data.TimeoutSet)
            {
                var timeout = data.timeout.HasValue
                    ? TimeSpan.FromMilliseconds(data.timeout.Value)
                    : default(TimeSpan?);
                configBuilder.WithTimeout(timeout);
            }
        }
        catch (ArgumentOutOfRangeException e) when ((data.timeout ?? 0) < 0 && e.ParamName == "value")
        {
            throw new DriverExceptionWrapper(e);
        }
    }

    [JsonConverter(typeof(BaseSessionTypeJsonConverter<SessionWriteTransactionType>))]
    public class SessionWriteTransactionType : BaseSessionType
    {
    }
}