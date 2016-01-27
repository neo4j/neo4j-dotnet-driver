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
using System.Runtime.Serialization;

namespace Neo4j.Driver.Exceptions
{
    [DataContract]
    public class Neo4jException : Exception
    {
        public Neo4jException()
        {
        }

        public Neo4jException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public Neo4jException(string code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }

        public string Code { get; set; }
    }

    [DataContract]
    public class ClientException : Neo4jException
    {
        public ClientException()
        {
        }

        public ClientException(string message) : base(null, message)
        {
        }

        public ClientException(string code, string message) : base(code, message)
        {
        }

        public ClientException(string code, string message, Exception innerException)
            : base(code, message, innerException)
        {
        }
    }

    [DataContract]
    public class TransientException : Neo4jException
    {
        public TransientException()
        {
        }

        public TransientException(string code, string message) : base(code, message)
        {
        }

        public TransientException(string code, string message, Exception innerException)
            : base(code, message, innerException)
        {
        }
    }

    [DataContract]
    public class DatabaseException : Neo4jException
    {
        public DatabaseException()
        {
        }

        public DatabaseException(string code, string message) : base(code, message)
        {
        }

        public DatabaseException(string code, string message, Exception innerException)
            : base(code, message, innerException)
        {
        }
    }
}