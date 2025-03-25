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

using System.Reflection;

namespace Neo4j.Driver.Internal.Mapping;

internal class DelegateMapper
{
    private static readonly IParameterMapper ParameterMapper = new ParameterMapper();

    internal static T MapWithMethodInfo<T>(IRecord record, MethodInfo mapFunction, object target)
    {
        var mapMethod = ParameterMapper.GetParameterMappedCall<T>(mapFunction, target);
        return mapMethod(record);
    }
}
