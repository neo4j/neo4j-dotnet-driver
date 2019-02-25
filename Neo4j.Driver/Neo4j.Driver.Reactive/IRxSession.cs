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
using System.Reactive;

namespace Neo4j.Driver
{
    public interface IRxSession : IRxRunnable
    {
        IObservable<IRxTransaction> BeginTransaction();

        IObservable<IRxTransaction> BeginTransaction(TransactionConfig txConfig);

        IRxResult Run(string statement, object parameters, TransactionConfig txConfig);

        IRxResult Run(Statement statement, TransactionConfig txConfig);

        IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work);

        IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work, TransactionConfig txConfig);

        IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work);

        IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work, TransactionConfig txConfig);

        IObservable<T> Close<T>();
    }
}