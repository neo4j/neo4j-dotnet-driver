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

using Neo4j.Driver.Bolt.Messages.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;

namespace Neo4j.Driver.Bolt.Messages.Types;

/// <summary>
/// Discriminated union for a decoded Bolt response message.
/// </summary>
public readonly struct BoltResponseMessage
{
    private readonly PackStreamStructView _payload;

    internal BoltResponseMessage(MessageKind kind, PackStreamStructView payload)
    {
        Kind = kind;
        _payload = payload;
    }

    public MessageKind Kind { get; }

    public SuccessMessageView AsSuccess() =>
        Kind == MessageKind.Success
            ? new SuccessMessageView(_payload)
            : throw new InvalidOperationException($"Message is {Kind}, not Success.");

    public RecordMessageView AsRecord() =>
        Kind == MessageKind.Record
            ? new RecordMessageView(_payload)
            : throw new InvalidOperationException($"Message is {Kind}, not Record.");

    public FailureMessageView AsFailure() =>
        Kind == MessageKind.Failure
            ? new FailureMessageView(_payload)
            : throw new InvalidOperationException($"Message is {Kind}, not Failure.");

    public IgnoredMessageView AsIgnored() =>
        Kind == MessageKind.Ignored
            ? new IgnoredMessageView(_payload)
            : throw new InvalidOperationException($"Message is {Kind}, not Ignored.");
}
