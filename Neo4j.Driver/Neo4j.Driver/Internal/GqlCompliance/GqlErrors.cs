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
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.GqlCompliance;

internal static class GqlErrors
{
    private const string UnknownNeo4JCode = "Neo.DatabaseError.General.UnknownError";
    private const string UnknownMessage = "An unknown error occurred";
    public const string UnknownGqlStatus = "50N42";
    public const string UnknownGqlStatusDescription = "error: general processing exception - unexpected error.";

    private const string ClientError = "CLIENT_ERROR";
    private const string DatabaseError = "DATABASE_ERROR";
    private const string TransientError = "TRANSIENT_ERROR";
    public const string UnknownError = "UNKNOWN";

    public static void FillGqlDefaults(FailureMessage message)
    {
        message.CodeInternal ??= UnknownNeo4JCode;
        message.Message ??= UnknownMessage;
        message.GqlStatus ??= UnknownGqlStatus;

        if (string.IsNullOrEmpty(message.GqlStatusDescription))
        {
            message.GqlStatusDescription = UnknownGqlStatusDescription + " " + message.Message;
        }

        message.GqlDiagnosticRecord ??= new Dictionary<string, object>();
        message.GqlDiagnosticRecord.FillMissingFrom(NewDefaultDiagnosticRecord());

        if (message.GqlDiagnosticRecord.TryGetValue("_classification", out var classification))
        {
            message.GqlRawClassification = classification.ToString();
            message.GqlClassification = UnknownError;
            
            if(classification is string and (ClientError or TransientError or DatabaseError))
            {
                message.GqlClassification = classification.ToString();
            }
        }
        else
        {
            message.GqlRawClassification = null;
            message.GqlClassification = UnknownError;
        }
    }

    public static Dictionary<string, object> NewDefaultDiagnosticRecord()
    {
        return new Dictionary<string, object>
        {
            ["OPERATION"] = "",
            ["OPERATION_CODE"] = "0",
            ["CURRENT_SCHEMA"] = "/"
        };
    }
}
