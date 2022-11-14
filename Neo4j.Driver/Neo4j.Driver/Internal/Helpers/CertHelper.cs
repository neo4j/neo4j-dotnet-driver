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
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Neo4j.Driver.Internal;

internal static class CertHelper
{
    public static bool CheckValidity(X509Certificate2 certificate, DateTime time)
    {
        return time > certificate.NotBefore && time < certificate.NotAfter;
    }

    public static bool FindCertificate(StoreLocation location, StoreName name, X509Certificate certificate)
    {
        using var store = new X509Store(name, location);
        store.Open(OpenFlags.ReadOnly);
        return FindCertificate(store.Certificates, certificate);
    }

    public static bool FindCertificate(X509Certificate2Collection store, X509Certificate certificate)
    {
        var matches = store.Find(
            X509FindType.FindByThumbprint,
            string.Concat(certificate.GetCertHash().Select(b => b.ToString("X2"))),
            false);

        return matches.Count > 0;
    }

    public static string ChainStatusToText(X509ChainStatus[] statuses)
    {
        return string.Join(Environment.NewLine, statuses.Select(ChainStatusToText));
    }

    public static string ChainStatusToText(X509ChainStatus status)
    {
        return string.IsNullOrEmpty(status.StatusInformation)
            ? $"[{status.Status}]"
            : $"[{status.Status}]: {status.StatusInformation}";
    }
}
