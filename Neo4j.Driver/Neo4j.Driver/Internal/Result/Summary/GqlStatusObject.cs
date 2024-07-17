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

using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Result;

internal class GqlStatusObject : IGqlStatusObject
{
    public static readonly IGqlStatusObject Success = new GqlStatusObject(
        "00000",
        "note: successful completion",
        new Dictionary<string, object>
        {
            { "CURRENT_SCHEMA", "/" },
            { "OPERATION", "" },
            { "OPERATION_CODE", "0" }
        });

    public static readonly IGqlStatusObject NoData = new GqlStatusObject(
        "02000",
        "note: no data",
        new Dictionary<string, object>
        {
            { "CURRENT_SCHEMA", "/" },
            { "OPERATION", "" },
            { "OPERATION_CODE", "0" }
        });

    public static readonly IGqlStatusObject NoDataUnknown = new GqlStatusObject(
        "02N42",
        "note: no data - unknown subcondition",
        new Dictionary<string, object>
        {
            { "CURRENT_SCHEMA", "/" },
            { "OPERATION", "" },
            { "OPERATION_CODE", "0" }
        });

    public static readonly IGqlStatusObject OmittedResult = new GqlStatusObject(
        "00001",
        "note: successful completion - omitted result",
        new Dictionary<string, object>
        {
            { "CURRENT_SCHEMA", "/" },
            { "OPERATION", "" },
            { "OPERATION_CODE", "0" }
        });

    public static readonly Dictionary<string, object> DefaultDiagnosticRecord = new Dictionary<string, object>
    {
        { "CURRENT_SCHEMA", "/" },
        { "OPERATION", "" },
        { "OPERATION_CODE", "0" }
    };

    private readonly string _gqlStatus;
    private readonly string _statusDescription;
    private readonly IDictionary<string, object> _diagnosticRecord;

    public GqlStatusObject(string gqlStatus, string statusDescription, IDictionary<string, object> diagnosticRecord)
    {
        _gqlStatus = gqlStatus ?? throw new ArgumentNullException(nameof(gqlStatus));
        _statusDescription = statusDescription ?? throw new ArgumentNullException(nameof(statusDescription));
        _diagnosticRecord = diagnosticRecord ?? throw new ArgumentNullException(nameof(diagnosticRecord));
    }

    public string GqlStatus => _gqlStatus;
    public string StatusDescription => _statusDescription;
    public IDictionary<string, object> DiagnosticRecord => _diagnosticRecord;

    public override bool Equals(object obj)
    {
        return obj is GqlStatusObject other &&
            _gqlStatus == other._gqlStatus &&
            _statusDescription == other._statusDescription &&
            _diagnosticRecord.Matches(other._diagnosticRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (_gqlStatus?.GetHashCode() ?? 0);
            hash = hash * 23 + (_statusDescription?.GetHashCode() ?? 0);
            hash = hash * 23 + (_diagnosticRecord != null ? _diagnosticRecord.GetHashCode() : 0);
            return hash;
        }
    }

    public override string ToString()
    {
        return
            $"GqlStatusObject{{gqlStatus='{_gqlStatus}', statusDescription='{_statusDescription}', " +
            $"diagnosticRecord={_diagnosticRecord.ToContentString()}}}";
    }
}
