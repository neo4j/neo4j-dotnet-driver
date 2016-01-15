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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.result
{
    public class ResultBuilder
    {
        //handles success

        void keys(String[] names)
        {
            
        }

        void record(object[] fields)
        {
            
        }

        public Result Build()
        {
            throw new NotImplementedException();
        }

        public void CollectMeta(IDictionary<string, object> meta)
        {
            if (meta == null)
                return;
            //
            foreach (var item in meta)
            {
                Debug.WriteLine("{0} -> {1}", item.Key, item.Value);
            }
        }
    }
}
