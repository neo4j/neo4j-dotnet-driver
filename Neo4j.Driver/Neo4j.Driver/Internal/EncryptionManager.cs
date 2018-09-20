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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    /// <summary>
    /// Manages how the driver will establish (encrypted) connections with the server.
    /// </summary>
    internal class EncryptionManager
    {
        private readonly EncryptionLevel _encryptionLevel;

        public EncryptionManager(){} // for test

        public EncryptionManager(EncryptionLevel level, TrustStrategy strategy, TrustManager trustManager, IDriverLogger logger)
        {
            _encryptionLevel = level;

            if (_encryptionLevel == EncryptionLevel.Encrypted)
            {
                if (trustManager == null)
                {
                    switch (strategy)
                    {
                        case V1.TrustStrategy.TrustAllCertificates:
                            trustManager = TrustManager.CreateInsecure(false);
                            break;
                        case V1.TrustStrategy.TrustSystemCaSignedCertificates:
                            trustManager = TrustManager.CreateChainTrust(true);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown trust strategy: {strategy}");
                    }
                }

                trustManager.Logger = logger;

                TrustManager = trustManager;
            }
        }

        public bool UseTls
        {
            get
            {
                switch (_encryptionLevel)
                {
                    case EncryptionLevel.Encrypted:
                        return true;
                    case EncryptionLevel.None:
                        return false;
                    default:
                        throw new NotSupportedException($"Unknown encryption level: {_encryptionLevel}");
                }
            }
        }

        public TrustManager TrustManager { get; }
    }
}
