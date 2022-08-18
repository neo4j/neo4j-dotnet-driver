// Copyright (c) 2002-2022 "Neo4j,"
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

namespace Neo4j.Driver.Tests.TestBackend;

public static class ProtocolTypes
{
    private static readonly HashSet<Type> types =
        new()
        {
            typeof(NewDriver),
            typeof(DriverClose),
            typeof(NewSession),
            typeof(SessionClose),
            typeof(AuthorizationToken),
            typeof(SessionRun),
            typeof(TransactionRun),
            typeof(TransactionCommit),
            typeof(TransactionRollback),
            typeof(TransactionClose),
            typeof(SessionReadTransaction),
            typeof(SessionWriteTransaction),
            typeof(SessionBeginTransaction),
            typeof(Result),
            typeof(ResultNext),
            typeof(ResultPeek),
            typeof(ResultList),
            typeof(ResultSingle),
            typeof(ResultConsume),
            typeof(RetryablePositive),
            typeof(RetryableNegative),
            typeof(ProtocolExceptionWrapper),
            typeof(SessionLastBookmarks),
            typeof(VerifyConnectivity),
            typeof(GetServerInfo),
            typeof(CheckMultiDBSupport),
            typeof(CheckDriverIsEncrypted),
            typeof(ResolverResolutionCompleted),
            typeof(StartTest),
            typeof(GetFeatures),
            typeof(GetRoutingTable),
            typeof(CypherTypeField)
        };

    public static void ValidateType(string typeName)
    {
        try
        {
            var objectType = Type.GetType(typeof(ProtocolTypes).Namespace + "." + typeName, true);
            ValidateType(objectType);
        }
        catch
        {
            throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {typeName}");
        }
    }

    public static void ValidateType(Type objectType)
    {
        if (!types.Contains(objectType))
            throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {objectType}");
    }
}