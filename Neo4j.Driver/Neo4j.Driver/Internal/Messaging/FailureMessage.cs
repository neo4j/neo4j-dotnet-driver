﻿// Copyright (c) "Neo4j"
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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class FailureMessage : IResponseMessage
{
    public FailureMessage(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }

    public void Dispatch(IResponsePipeline pipeline)
    {
        pipeline.OnFailure(Code, Message);
    }

    public IPackStreamSerializer Serializer => FailureMessageSerializer.Instance;

    public override string ToString()
    {
        return $"FAILURE code={Code}, message={Message}";
    }
}
