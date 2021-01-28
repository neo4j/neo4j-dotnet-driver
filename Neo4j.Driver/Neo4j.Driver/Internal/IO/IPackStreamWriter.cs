﻿// Copyright (c) "Neo4j"
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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.IO
{
    internal interface IPackStreamWriter
    {
        void Write(object value);
        void Write(string value);
        void Write(char value);
        void Write(int value);
        void Write(long value);
        void Write(double value);
        void Write(bool value);
        void Write(byte[] value);
        void WriteNull();
        void Write(IDictionary value);
        void Write(IList value);
        void WriteListHeader(int size);
        void WriteStructHeader(int size, byte signature);
        void WriteMapHeader(int size);
    }
}
