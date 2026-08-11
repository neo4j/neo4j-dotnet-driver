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

using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;

namespace Neo4j.Driver.TestKitBackend.Expectations;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class ReverseRoundTrip : IReverseRoundTrip
{
    private readonly IExpectationStore _expectations;
    private readonly IResponseWriter _writer;

    public ReverseRoundTrip(IExpectationStore expectations, IResponseWriter writer)
    {
        _expectations = expectations;
        _writer = writer;
    }

    public async Task<T> SendExpectingAsync<T>(IProtocolMessage message, string key)
    {
        var value = _expectations.Expect<T>(key);
        await _writer.WriteAsync(message);
        return await value;
    }

    public Task<T> SendExpectingAsync<T>(ICorrelatedRequest message)
    {
        message.Id = Guid.NewGuid().ToString();
        return SendExpectingAsync<T>(message, message.Id);
    }
}
