// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Used by routing driver to resolve the initial address used to create the driver.
    /// Such resolution happens: 1) during the very first rediscovery when driver is created.
    /// 2) when all the known routers from the current routing table have failed and driver needs to fallback to the initial address.
    /// </summary>
    public interface IServerAddressResolver
    {
        /// <summary>
        /// Given a server address with host name and port defined in <see cref="ServerAddress"/>,
        /// returns the resolved server addresses with host name and port saved in a set of <see cref="ServerAddress"/>.
        /// </summary>
        /// <param name="address">The server address to resolve.</param>
        /// <returns>Resolved server addresses in a set.</returns>
        ISet<ServerAddress> Resolve(ServerAddress address);
    }

    /// <summary>
    /// A server address consists of <see cref="ServerAddress.Host"/> and <see cref="ServerAddress.Port"/>.
    /// This address specifies where the driver to find the server.
    /// </summary>
    public sealed class ServerAddress : IEquatable<ServerAddress>
    {
        /// <summary>
        /// Create a server address with host name and port number.
        /// </summary>
        /// <param name="host">The host name of the server address.</param>
        /// <param name="port">The port number of the server address.</param>
        public static ServerAddress From(string host, int port)
        {
            return new ServerAddress(host, port);
        }

        /// <summary>
        /// Create a server address from a <see cref="Uri"/>.
        /// Fields <see cref="Uri.Host"/> and <see cref="Uri.Port"/> will be used to create the server address.
        /// </summary>
        /// <param name="uri">The input uri to read host name and port number from.</param>
        public static ServerAddress From(Uri uri)
        {
            return new ServerAddress(uri.Host, uri.Port);
        }

        private ServerAddress(string host, int port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Gets the host name of the server address.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the port number of the server address.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the
        /// value of the specified <see cref="ServerAddress"/> instance.
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(ServerAddress other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Host.Equals(other.Host) && Port == other.Port;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="ServerAddress"/> and
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ServerAddress address && Equals(address);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Host.GetHashCode();
                hashCode = (hashCode * 397) ^ Port;
                return hashCode;
            }
        }

        /// <summary>
        /// Print the content of the server address.
        /// </summary>
        /// <returns>A string representation of the server address.</returns>
        public override string ToString()
        {
            return $"{nameof(Host)}: {Host}, {nameof(Port)}: {Port}";
        }
    }

    // simply pass through the server address as it is in the return resolved address set.
    internal class PassThroughServerAddressResolver : IServerAddressResolver
    {
        public ISet<ServerAddress> Resolve(ServerAddress address)
        {
            return new HashSet<ServerAddress> {address};
        }
    }
}