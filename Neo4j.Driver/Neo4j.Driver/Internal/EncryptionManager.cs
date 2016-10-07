// Copyright (c) 2002-2016 "Neo Technology,"
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

        public EncryptionManager(EncryptionLevel level, TrustStrategy strategy, ILogger logger)
        {
            _encryptionLevel = level;

            if (_encryptionLevel != EncryptionLevel.None)
            {
                switch (strategy.ServerTrustStrategy())
                {
                    case V1.TrustStrategy.Strategy.TrustOnFirstUse:
                        TrustStrategy = new TrustOnFirstUse(logger, strategy.FileName());
                        break;
                    case V1.TrustStrategy.Strategy.TrustSystemCaSignedCertificates:
                        TrustStrategy = new TrustSystemCaSignedCertificates(logger);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown trust strategy: {strategy}");
                }
            }
        }
        
        public bool UseTls(Uri uri)
        {
            switch (_encryptionLevel)
            {
                case EncryptionLevel.None:
                    return false;
                case EncryptionLevel.Encrypted:
                    return true;
                case EncryptionLevel.EncryptedNonLocal:
                    return !uri.IsLoopback;
                default:
                    throw new InvalidOperationException($"Unknown encryption level {_encryptionLevel}");
            }
        }

        public ITrustStrategy TrustStrategy { get; }
    }
}
