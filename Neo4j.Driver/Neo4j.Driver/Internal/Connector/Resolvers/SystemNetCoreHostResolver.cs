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

using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Connector.Resolvers;

internal class SystemNetCoreHostResolver : IHostResolver
{
    private readonly IHostResolver _fallBackResolver;
    private readonly MethodInfo _getHostByNameMethod;

    public SystemNetCoreHostResolver()
        : this(new SystemHostResolver())
    {
    }

    public SystemNetCoreHostResolver(IHostResolver fallBackResolver)
    {
        // Force native socket library initialization
        Dns.GetHostName();

        _fallBackResolver = fallBackResolver;
        var dnsType = typeof(Dns).GetTypeInfo();
        _getHostByNameMethod =
            DiscoverMethod(dnsType, "InternalGetHostByName", new[] { typeof(string), typeof(bool) });
    }

    public IPAddress[] Resolve(string hostname)
    {
        if (_getHostByNameMethod != null)
        {
            var result = _getHostByNameMethod.Invoke(null, new object[] { hostname, true });

            if (result is IPHostEntry resolved)
            {
                return resolved.AddressList;
            }
        }

        return _fallBackResolver.Resolve(hostname);
    }

    public Task<IPAddress[]> ResolveAsync(string hostname)
    {
        if (_getHostByNameMethod != null)
        {
            return Task.FromResult(Resolve(hostname));
        }

        return _fallBackResolver.ResolveAsync(hostname);
    }

    private static MethodInfo DiscoverMethod(TypeInfo type, string name, Type[] parameters)
    {
        return type
            .GetDeclaredMethods(name)
            .SingleOrDefault(m => parameters.SequenceEqual(m.GetParameters().Select(p => p.ParameterType)));
    }
}
