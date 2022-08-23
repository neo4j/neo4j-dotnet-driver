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

using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionClose : ProtocolObject
{
    public TransactionCloseDataType data { get; set; } = new();

    public override async Task ProcessAsync(Controller controller)
    {
        var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
        await transactionWrapper.Transaction.DisposeAsync();
    }

    public override async Task ReactiveProcessAsync(Controller controller)
    {
        var transactionWrapper = controller.ReactiveTransactionManager.FindTransaction(data.txId);
        await transactionWrapper.Transaction.Close<Unit>().IsEmpty();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Transaction", UniqueId).Encode();
    }

    public class TransactionCloseDataType
    {
        public string txId { get; set; }
    }
}