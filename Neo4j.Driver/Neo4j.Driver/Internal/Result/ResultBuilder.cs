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
        //private IDictionary<string, dynamic> _meta;
        private string[] _keys = new string[0];
        private IList<Record> _records = new List<Record>(); 

        public void Record(dynamic[] fields)
        {
            Record record = new Record( _keys, fields);
            _records.Add( record );
        }

        public ResultCursor Build()
        {
            return new ResultCursor(_records, _keys);
        }

        public void CollectMeta(IDictionary<string, object> meta)
        {
            if (meta == null)
            {
                return;
            }
            //_meta = meta;

            CollectKeys( meta );
        }

        private void CollectKeys(IDictionary<string, object> meta)
        {
            const string fieldsName = "fields";
            if (meta.ContainsKey(fieldsName))
            {
                var keys = (meta[fieldsName] as IList<object>)?.Cast<string>();
                if (keys == null)
                {
                    _keys = new string[0];
                    return;
                }

                _keys = keys.ToArray();
            }
        }
    }
}
