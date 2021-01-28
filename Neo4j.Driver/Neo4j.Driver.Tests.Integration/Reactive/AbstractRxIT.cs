﻿// Copyright (c) "Neo4j"
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Reactive;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.Reactive.Utils;

namespace Neo4j.Driver.IntegrationTests.Reactive
{
    [Collection(SAIntegrationCollection.CollectionName)]
    public abstract class AbstractRxIT : AbstractRxTest, IDisposable
    {
        private bool _disposed = false;
        private readonly List<IRxSession> _sessions = new List<IRxSession>();

        protected IStandAlone Server { get; }
        protected Uri ServerEndPoint { get; }
        protected IAuthToken AuthToken { get; }

        ~AbstractRxIT() => Dispose(false);

        protected AbstractRxIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output)
        {
            Server = fixture.StandAloneSharedInstance;
            ServerEndPoint = Server.BoltUri;
            AuthToken = Server.AuthToken;
        }

        protected IRxSession NewSession()
        {
            var session = Server.Driver.RxSession();
            _sessions.Add(session);
            return session;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
		{
            if (_disposed)
                return;

            if(disposing)
			{  
                _sessions.ForEach(x => x.Close<Unit>().WaitForCompletion());
                _sessions.Clear();
            }

            //Mark as disposed
            _disposed = true;
        }
    }
}