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
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal;

internal class LoggingInterceptor : IResolutionInterceptor
{
    private ILoggerFactory _loggerFactory;

    public LoggingInterceptor(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public bool TryResolve(Type serviceType, Type requestingType, IServiceResolver resolver, out object service)
    {
        if (serviceType == typeof(ILogger))
        {
            var tracker = resolver.Resolve<ILoggingContextTracker>();
            service = _loggerFactory.GetLoggerForType(requestingType, tracker);
            return true;
        }
        
        service = null;
        return false;
    }
}
