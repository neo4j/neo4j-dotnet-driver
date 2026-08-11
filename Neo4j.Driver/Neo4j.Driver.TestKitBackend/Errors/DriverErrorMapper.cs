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

using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Errors;

internal interface IDriverErrorMapper
{
    DriverErrorResponse Map(Exception exception);
}

internal class DriverErrorMapper : IDriverErrorMapper
{
    private readonly IObjectStore _objectStore;
    private readonly IExceptionTypeMapper _exceptionTypeMapper;
    private readonly INativeToCypherMapper _cypherMapper;

    public DriverErrorMapper(
        IObjectStore objectStore,
        IExceptionTypeMapper exceptionTypeMapper,
        INativeToCypherMapper cypherMapper)
    {
        _objectStore = objectStore;
        _exceptionTypeMapper = exceptionTypeMapper;
        _cypherMapper = cypherMapper;
    }

    public DriverErrorResponse Map(Exception exception)
    {
        var registered = _objectStore.Register(exception);
        var errorType = _exceptionTypeMapper.Map(exception);

        return exception switch
        {
            Neo4jException nex => new DriverErrorResponse
            {
                Id = registered.Id,
                ErrorType = errorType,
                Msg = nex.Message,
                Code = nex.Code ?? errorType,
                Retryable = nex.IsRetriable,
                GqlStatus = nex.GqlStatus,
                StatusDescription = nex.GqlStatusDescription,
                Classification = nex.GqlClassification,
                RawClassification = nex.GqlRawClassification,
                DiagnosticRecord = nex.GqlDiagnosticRecord?
                    .ToDictionary(kv => kv.Key, kv => _cypherMapper.Map(kv.Value)),
                Cause = MapCause(nex.InnerException)
            },
            _ => new DriverErrorResponse
            {
                Id = registered.Id,
                ErrorType = errorType,
                Msg = exception.Message,
                Code = errorType,
                Retryable = false
            }
        };
    }

    private GqlErrorResponse? MapCause(Exception? cause)
    {
        if (cause is not Neo4jException gqlCause)
        {
            return null;
        }

        return new GqlErrorResponse
        {
            Msg = gqlCause.Message,
            GqlStatus = gqlCause.GqlStatus,
            StatusDescription = gqlCause.GqlStatusDescription,
            Classification = gqlCause.GqlClassification,
            RawClassification = gqlCause.GqlRawClassification,
            DiagnosticRecord = gqlCause.GqlDiagnosticRecord?
                .ToDictionary(kv => kv.Key, kv => _cypherMapper.Map(kv.Value)),
            Cause = MapCause(gqlCause.InnerException)
        };
    }
}
