// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Linq;
using System.Reflection;

namespace Neo4j.Driver.Internal.ExceptionHandling;

internal class ClientErrorExceptionFactory
{
    private record FactoryInfo(string Code, Func<string, string, Exception, Neo4jException> ExceptionFactory);
    private readonly List<FactoryInfo> _exceptionFactories = new();
    private readonly SimpleWildcardHelper _simpleWildcardHelper = new();

    public ClientErrorExceptionFactory()
    {
        // get all the types
        var codesAndExceptions = GetCodesAndExceptions();
        codesAndExceptions.Sort(CompareByCode);
        BuildExceptionFactories(codesAndExceptions);
    }

    private List<(string code, Type exceptionType)> GetCodesAndExceptions()
    {
        var exceptionTypes = GetAllNeo4jExceptions();

        return exceptionTypes
            .Select(
                exceptionType => new
                {
                    exceptionType,
                    attr = exceptionType.GetCustomAttribute<ClientErrorCodeAttribute>()
                })
            .Where(t => t.attr is not null)
            .Select(t => (t.attr.Code, t.exceptionType))
            .ToList();
    }

    private static IEnumerable<Type> GetAllNeo4jExceptions()
    {
        var type = typeof(Neo4jException);
        var assembly = type.Assembly;
        var types = assembly.GetExportedTypes().Where(t => type.IsAssignableFrom(t));
        return types;
    }

    private int CompareByCode((string code, Type exceptionType) x, (string code, Type exceptionType) y)
    {
        // x comes before y if y matches x - this would happen if:
        // x = Error.Specific
        // y = Error.*
        // this means that less-specific wildcards are at the end of the list, so the first
        // matching wildcard will always be the most specific

        if (_simpleWildcardHelper.StringMatches(x.code, y.code))
        {
            return -1;
        }

        if (_simpleWildcardHelper.StringMatches(y.code, x.code))
        {
            return 1;
        }

        return 0;
    }

    private void BuildExceptionFactories(IEnumerable<(string code, Type exceptionType)> codesAndExceptions)
    {
        foreach (var (code, type) in codesAndExceptions)
        {
            Func<string, string, Exception, Neo4jException> factory;
            if (type.GetConstructor(new[] { typeof(string), typeof(string), typeof(Exception) }) is {} threeParamCtr)
            {
                factory = (c, m, x) => (Neo4jException)threeParamCtr.Invoke(new object[] { c, m, x });
            }
            else if (type.GetConstructor(new[] { typeof(string), typeof(Exception) }) is {} twoParamCtr)
            {
                factory = (_, m, x) => (Neo4jException)twoParamCtr.Invoke(new object[] { m, x });
            }
            else if (type.GetConstructor(new[] { typeof(string) }) is {} oneParamCtr)
            {
                factory = (_, m, _) => (Neo4jException)oneParamCtr.Invoke(new object[] { m });
            }
            else
            {
                continue;
            }

            _exceptionFactories.Add(new FactoryInfo(code, factory));
        }
    }

    public Neo4jException GetException(string code, string message, Exception innerException = null)
    {
        var factoryInfo = _exceptionFactories.FirstOrDefault(f => _simpleWildcardHelper.StringMatches(code, f.Code));
        var exception = factoryInfo is null
            ? new Neo4jException(code, message, innerException)
            : factoryInfo.ExceptionFactory(code, message, innerException);

        exception.Code = code;
        return exception;
    }
}
