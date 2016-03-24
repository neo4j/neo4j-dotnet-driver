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

using System.Collections.Generic;
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     Neo4j installer
    /// </summary>
    public interface INeo4jInstaller
    {
        /// <summary>
        ///     The Neo4j binaries folder.
        /// </summary>
        /// <remarks>
        ///     This only defined if <see cref="DownloadNeo4j" /> has been called.
        /// </remarks>
        DirectoryInfo Neo4jHome { get; }

        /// <summary>
        ///     Downloads the Neo4j binaries
        /// </summary>
        /// <returns></returns>
        void DownloadNeo4j();

        /// <summary>
        ///     Installs Neo4j server as a service (Windows only)
        /// </summary>
        void InstallServer();

        /// <summary>
        ///     Starts the Neo4j server (Any platform)
        /// </summary>
        void StartServer();

        /// <summary>
        ///     Tops the Neo4j server (ANy platform)
        /// </summary>
        void StopServer();

        /// <summary>
        ///     Uninstalls the Neo4j server (Windows only) <see cref="InstallServer" />
        /// </summary>
        void UninstallServer();

        /// <summary>
        ///     Updates the Neo4j server settings
        /// </summary>
        /// <param name="keyValuePair"></param>
        void UpdateSettings(IDictionary<string, string> keyValuePair);
    }
}