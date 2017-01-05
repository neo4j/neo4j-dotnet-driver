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
using System.Runtime.Serialization;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// The base class for all Neo4j exceptions.
    /// </summary>
    [DataContract]
    public class Neo4jException : Exception
    {
        public Neo4jException()
        {
        }

        public Neo4jException(string message) : this(null, message)
        {
        }
        public Neo4jException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public Neo4jException(string message, Exception innerException)
            : this(null, message, innerException)
        {
        }

        public Neo4jException(string code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }

        /// <summary>
        /// Gets or sets the code of a Neo4j exception.
        /// </summary>
        public string Code { get; set; }
    }

    /// <summary>
    /// A <see cref="ClientException"/> indicates that the client has carried out an operation incorrectly.
    /// The error code provided can be used to determine further detail for the problem.
    /// </summary>
    [DataContract]
    public class ClientException : Neo4jException
    {
        public ClientException()
        {
        }

        public ClientException(string message) : base(message)
        {
        }

        public ClientException(string code, string message) : base(code, message)
        {
        }

        public ClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ClientException(string code, string message, Exception innerException)
            : base(code, message, innerException)
        {
        }
    }

    /// <summary>
    /// A <see cref="TransientException"/> signals a failed operation that may be able to succeed 
    /// if this operation is retried without any intervention by application-level functionality. 
    /// The error code provided can be used to determine further details for the problem.
    /// </summary>
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

    /// <summary>
    /// A <see cref="DatabaseException"/> indicates that there is a problem within the underlying database.
    /// The error code provided can be used to determine further detail for the problem.
    /// </summary>
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

    /// <summary>
    ///  A <see cref="ServiceUnavailableException"/> indicates that the driver cannot communicate with the cluster.
    /// </summary>
    [DataContract]
    public class ServiceUnavailableException : Neo4jException
    {
        public ServiceUnavailableException(string message) : base(message)
        {
        }

        public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// A <see cref="SessionExpiredException"/> indicates that the session can no longer satisfy the criteria under which it was acquired,
    /// e.g. a server no longer accepts write requests.
    ///
    /// A new session needs to be acquired from the driver and all actions taken on the expired session must be replayed.
    /// </summary>
    [DataContract]
    public class SessionExpiredException : Neo4jException
    {
        public SessionExpiredException(string message) : base(message)
        {
        }

        public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}