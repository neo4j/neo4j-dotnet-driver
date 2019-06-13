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
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;

namespace Neo4j.Driver.Reactive
{
    public static class Extensions
    {
        public static void SubscribeAndDiscard<T>(this IObservable<T> observable, int millisecondsTimeout = -1)
        {
            var waiter = new ManualResetEventSlim(false);
            using (observable.Finally(() => waiter.Set()).Subscribe(Observer.Create<T>(t => { })))
            {
                waiter.Wait(millisecondsTimeout);
            }
        }

        public static ITestableObserver<T> SubscribeAndWait<T>(this IObservable<T> observable,
            ITestableObserver<T> observer, int millisecondsTimeout = -1)
        {
            var waiter = new ManualResetEventSlim(false);
            using (observable.Finally(() => waiter.Set()).Subscribe(observer))
            {
                waiter.Wait(millisecondsTimeout);
            }

            return observer;
        }

        public static void AssertEqual<T>(this ITestableObserver<T> observer,
            params Recorded<Notification<T>>[] notifications)
        {
            observer.Messages.AssertEqual(notifications);
        }

        public static void AssertEqual<T>(this ITestableObserver<T> observer1,
            ITestableObserver<T> observer2)
        {
            observer1.Messages.AssertEqual(observer2.Messages);
        }
    }
}