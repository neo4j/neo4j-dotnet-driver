// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.V1
{
    public class TransactionConfig
    {
        public static readonly TransactionConfig Empty = new TransactionConfig();
        private IDictionary<string, object> _metadata = PackStream.EmptyDictionary;
        private TimeSpan _timeout = TimeSpan.Zero;

        public TimeSpan Timeout
        {
            get => _timeout;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException( nameof(Timeout), value, "Transaction timeout should not be zero or negative.");
                }
                _timeout = value;
            }
        }

        public IDictionary<string, object> Metadata
        {
            get => _metadata;
            set => _metadata = value ?? throw new ArgumentNullException(nameof(Metadata), "Transaction metadata should not be null");
        }
    }
}