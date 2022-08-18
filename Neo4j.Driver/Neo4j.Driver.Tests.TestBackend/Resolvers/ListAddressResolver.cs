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
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ListAddressResolver : IServerAddressResolver
{
    private readonly Controller _control;
    private readonly Uri _uri;

    public ListAddressResolver(Controller control, string uri)
    {
        _control = control;
        _uri = new Uri(uri);
    }

    public ISet<ServerAddress> Resolve(ServerAddress address)
    {
        var errorMessage =
            "A ResolverResolutionCompleted request is expected straight after a ResolverResolutionRequired response is sent";
        var response = new ProtocolResponse("ResolverResolutionRequired",
                new
                {
                    id = ProtocolObjectManager.GenerateUniqueIdString(),
                    address = _uri.Host + ":" + _uri.Port
                })
            .Encode();

        //Send the ResolverResolutionRequired response
        _control.SendResponseAsync(response);

        //Read the ResolverResolutionCompleted request, throw if another type of request has come in
        var result = _control.TryConsumeStreamObjectAsync<ResolverResolutionCompleted>().GetAwaiter().GetResult();
        if (result is null)
            throw new NotSupportedException(errorMessage);

        //Return a IServerAddressResolver instance that's Resolve method uses the addresses in the ResolverResolutionCompleted request.
        return new HashSet<ServerAddress>(
            result.data.addresses.Select(x =>
            {
                var split = x.Split(':');
                return ServerAddress.From(split[0], Convert.ToInt32(split[1]));
            }));
    }
}