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
using System.Linq;
using Neo4j.Driver.Tests.TestBackend.Protocol;
using Neo4j.Driver.Tests.TestBackend.Protocol.SupportFunctions;

namespace Neo4j.Driver.Tests.TestBackend.Resolvers;

internal class ListAddressResolver : IServerAddressResolver
{
    private readonly ServerAddress[] servers;

    public ListAddressResolver(Controller control, string uri)
    {
        Control = control;
        Uri = new Uri(uri);
    }

    public ListAddressResolver(params ServerAddress[] servers)
    {
        this.servers = servers;
    }

    private Controller Control { get; }
    private Uri Uri { get; }

    public ISet<ServerAddress> Resolve(ServerAddress address)
    {
        var errorMessage =
            "A ResolverResolutionCompleted request is expected straight after a ResolverResolutionRequired reponse is sent";

        var response = new ProtocolResponse(
                "ResolverResolutionRequired",
                new
                {
                    id = ProtocolObjectManager.GenerateUniqueIdString(),
                    address = Uri.Host + ":" + Uri.Port
                })
            .Encode();

        //Send the ResolverResolutionRequired response
        Control.SendResponse(response).ConfigureAwait(false);

        //Read the ResolverResolutionCompleted request, throw if another type of request has come in
        var result = Control.TryConsumeStreamObjectOfType<ResolverResolutionCompleted>().Result;
        if (result is null)
        {
            throw new NotSupportedException(errorMessage);
        }

        //Return a IServerAddressResolver instance thats Resolve method uses the addresses in the ResolverResolutionoCompleted request.
        return new HashSet<ServerAddress>(
            result
                .data
                .addresses
                .Select(
                    x =>
                    {
                        var split = x.Split(':');
                        return ServerAddress.From(split[0], Convert.ToInt32(split[1]));
                    }));
    }
}
