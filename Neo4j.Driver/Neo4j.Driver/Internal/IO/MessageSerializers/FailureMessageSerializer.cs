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

using System.Collections.Generic;
using Neo4j.Driver.Internal.GqlCompliance;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal sealed class FailureMessageSerializer : ReadOnlySerializer, IPackStreamMessageDeserializer
{
    internal static FailureMessageSerializer Instance = new();

    private static readonly byte[] StructTags = { MessageFormat.MsgFailure };
    public override byte[] ReadableStructs => StructTags;

    public IResponseMessage DeserializeMessage(
        BoltProtocolVersion boltProtocolVersion,
        SpanPackStreamReader packStreamReader)
    {
        var values = packStreamReader.ReadMap();
        var majorVersion = boltProtocolVersion.MajorVersion;
        return BuildFailureMessage(values, majorVersion);
    }

    public override object Deserialize(
        BoltProtocolVersion boltProtocolVersion,
        PackStreamReader packStreamReader,
        byte _,
        long __)
    {
        var values = packStreamReader.ReadMap();
        var majorVersion = boltProtocolVersion.MajorVersion;
        return BuildFailureMessage(values, majorVersion);
    }

    internal static FailureMessage BuildFailureMessage(IReadOnlyDictionary<string, object> values, int majorVersion)
    {
        var response = new FailureMessage();
        foreach (var (key, value) in values)
        {
            switch (key)
            {
                case "neo4j_code":
                case "code":
                    response.Code = FixCodeForBolt5(majorVersion, value?.ToString() ?? string.Empty);
                    break;

                case "message":
                    response.Message = value?.ToString();
                    break;

                case "gql_status":
                    response.GqlStatus = value?.ToString();
                    break;

                case "description":

                    response.GqlStatusDescription = value?.ToString();
                    break;

                case "diagnostic_record":
                    response.GqlDiagnosticRecord = (Dictionary<string, object>)value;
                    break;

                case "cause":
                    response.GqlCause = BuildFailureMessage((Dictionary<string, object>)value, majorVersion);
                    break;

                default:
                    throw new ProtocolException($"Unexpected key: {key} in FAILURE message.");
            }
        }

        GqlErrors.FillGqlDefaults(response);
        return response;
    }

    private static string FixCodeForBolt5(int majorVersion, string code)
    {
        // codes were fixed in bolt 5, so we need to interpret these codes.
        if (majorVersion >= 5)
        {
            return code;
        }

        return code switch
        {
            "Neo.TransientError.Transaction.Terminated" => "Neo.ClientError.Transaction.Terminated",
            "Neo.TransientError.Transaction.LockClientStopped" => "Neo.ClientError.Transaction.LockClientStopped",
            _ => code
        };
    }
}
