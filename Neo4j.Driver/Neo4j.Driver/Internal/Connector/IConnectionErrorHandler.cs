// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.IO;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface IConnectionErrorHandler
    {
        /// <summary>
        /// Define what to do when a connection error happens when sending and receiving data
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        Exception OnConnectionError(Exception e);
        /// <summary>
        /// Define what to do when a failure received from the server after data received from the server
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        Neo4jException OnServerError(Neo4jException e);
    }
}