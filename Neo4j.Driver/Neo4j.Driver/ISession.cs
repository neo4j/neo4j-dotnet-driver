//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver
{

    /// <summary>
    /// A live session with a Neo4j instance.
    /// <br/>
    /// Sessions serve two purposes. For one, they are an optimization. By keeping state on the database side, we can
    /// avoid re-transmitting certain metadata over and over.
    /// <br/>
    /// Sessions also serve a role in transaction isolation and ordering semantics. Neo4j requires
    /// "sticky sessions", meaning all requests within one session must always go to the same Neo4j instance.
    /// <br/>
    /// Session objects are not thread safe, if you want to run concurrent operations against the database,
    /// simply create multiple sessions objects.
    /// </summary>
    public interface ISession : IDisposable
    {
        // TODO
        Result Run(string statement, IDictionary<string, object> statementParameters = null);
    }
}