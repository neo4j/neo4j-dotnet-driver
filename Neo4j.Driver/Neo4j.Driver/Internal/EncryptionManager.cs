﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal;

/// <summary>Manages how the driver will establish (encrypted) connections with the server.</summary>
internal class EncryptionManager
{
    public EncryptionManager(bool useTls, TrustManager trustManager)
    {
        UseTls = useTls;
        TrustManager = trustManager;
    }

    public bool UseTls { get; }
    public TrustManager TrustManager { get; }

    public static EncryptionManager Create(
        Uri uri,
        EncryptionLevel? level,
        TrustManager trustManager,
        ILogger logger)
    {
        var configured = level.HasValue || trustManager != null;
        if (configured)
        {
            AssertSimpleUriScheme(uri, level, trustManager);
            return CreateFromConfig(level, trustManager, logger);
        }

        return CreateFromUriScheme(uri, logger);
    }

    private static EncryptionManager CreateFromUriScheme(Uri uri, ILogger logger)
    {
        // let the uri scheme to decide
        return Neo4jUri.ParseUriSchemeToEncryptionManager(uri, logger);
    }

    private static void AssertSimpleUriScheme(Uri uri, EncryptionLevel? encryptionLevel, TrustManager trustManager)
    {
        if (!Neo4jUri.IsSimpleUriScheme(uri))
        {
            throw new ArgumentException(
                "The encryption and trust settings cannot both be set via uri scheme and driver configuration. " +
                $"uri scheme = {uri.Scheme}, encryption = {encryptionLevel}, trust = {trustManager}");
        }
    }

    public static EncryptionManager CreateFromConfig(
        EncryptionLevel? nullableLevel,
        TrustManager trustManager,
        ILogger logger)
    {
        var encrypted = ParseEncrypted(nullableLevel);
        if (encrypted && trustManager == null)
        {
            return new EncryptionManager(true, CreateSecureTrustManager(logger));
        }
        
        if (trustManager.Logger == null)
        {
            // likely to be true, as this is passed in by a user and logger is internal so we should set it.
            trustManager.Logger = logger;
        }
        
        return new EncryptionManager(encrypted, trustManager);
    }

    private static bool ParseEncrypted(EncryptionLevel? nullableLevel)
    {
        var level = nullableLevel.GetValueOrDefault(EncryptionLevel.None);
        switch (level)
        {
            case EncryptionLevel.Encrypted:
                return true;

            case EncryptionLevel.None:
                return false;

            default:
                throw new NotSupportedException($"Unknown encryption level: {level}");
        }
    }

    public static TrustManager CreateSecureTrustManager(ILogger logger)
    {
        var trustManager = TrustManager.CreateChainTrust();
        trustManager.Logger = logger;
        return trustManager;
    }

    public static TrustManager CreateInsecureTrustManager(ILogger logger)
    {
        var trustManager = TrustManager.CreateInsecure();
        trustManager.Logger = logger;
        return trustManager;
    }
}
