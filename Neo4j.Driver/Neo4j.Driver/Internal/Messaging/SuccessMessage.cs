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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Messaging
{
    internal class SuccessMessage : IResponseMessage
    {
        private readonly IDictionary<string, object> _meta;

        public SuccessMessage(IDictionary<string, object> meta)
        {
            _meta = meta;
        }

        public override string ToString()
        {
            return $"SUCCESS {_meta.ValueToString()}";
        }

        public void Dispatch(IMessageResponseHandler messageResponseHandler)
        {
            messageResponseHandler.HandleSuccessMessage(_meta);
        }
    }
}