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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface ITrustStrategy
    {
        bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors);
    }

    internal class TrustSystemCaSignedCertificates : ITrustStrategy
    {
        private readonly ILogger _logger;
        public TrustSystemCaSignedCertificates(ILogger logger)
        {
            _logger = logger;
        }
        public bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    _logger?.Error("Server name mismatch.");
                    return false;
                case SslPolicyErrors.RemoteCertificateNotAvailable:
                    _logger?.Error("Certificate not available.");
                    return false;
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    _logger?.Error("Certificate validation failed.");
                    return false;
            }

            _logger?.Debug("Authentication succeeded.");
            return true;
        }
    }

    internal class TrustOnFirstUse : ITrustStrategy
    {
        public static readonly string DefaultKnownHostsFilePath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".neo4j");
        private readonly string _knownHostsFilePath;
        private readonly IDictionary<string, string> _knownHosts = new Dictionary<string, string>();
        private readonly ILogger _logger;

        internal IDictionary<string, string> KnownHost => new ReadOnlyDictionary<string, string>(_knownHosts);

        public TrustOnFirstUse(ILogger logger, string knownHostsFilePath = null)
        {
            _logger = logger;
            _knownHostsFilePath = knownHostsFilePath ?? DefaultKnownHostsFilePath;
            LoadFromKnownHost();
        }

        private void LoadFromKnownHost()
        {
            if (!File.Exists(_knownHostsFilePath))
            {
                return;
            }

            using (var reader = File.OpenText(_knownHostsFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        var strings = line.Split(' ');
                        if (strings.Length == 2)
                        {
                            _knownHosts.Add(strings[0], strings[1]);
                            return;
                        }
                    }
                }
            }
        }

        public bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors)
        {
            var serverId = uri.ToString();
            var fingerprint = Fingerprint(certificate);
            
            if (!_knownHosts.ContainsKey(serverId))
            {
                // lock to make sure only one thread is appending the knownHost file
                lock (_knownHosts)
                {
                    if (!_knownHosts.ContainsKey(serverId)) // check again if still no one has appened the same
                    {
                        _knownHosts.Add(serverId, fingerprint);
                        SaveToKnownHost(serverId, fingerprint);
                    }
                }
            }
            if (!_knownHosts[serverId].Equals(fingerprint))
            {
                _logger?.Error(
                    $"Unable to connect to neo4j at `{serverId}`, because the certificate the server uses has changed. " +
                    "This is a security feature to protect against man-in-the-middle attacks.\n" +
                    "If you trust the certificate the server uses now, simply remove the line that starts with " +
                    $"`{serverId}` in the file `{_knownHostsFilePath}`.\n" +
                    $"The old certificate saved in file is:\n{fingerprint}\nThe New certificate received is:\n{fingerprint}");
                return false;
            }

            _logger?.Debug("Authentication succeeded.");
            return true;
        }

        private void SaveToKnownHost(string serverId, string fingerprint)
        {
            _logger?.Info($"Adding {fingerprint} as known and trusted certificate for {serverId}.");

            CreateFileRecursively(_knownHostsFilePath);
            using (var writer = File.AppendText(_knownHostsFilePath))
            {
                writer.WriteLine($"{serverId} {fingerprint}");
            }
        }

        private static string Fingerprint(X509Certificate certificate)
        {
            var data = certificate.GetCertHash();
            using (var sha512 = SHA512.Create())
            {
                return sha512.ComputeHash(data).ToHexString("");
            }
        }

        private static void CreateFileRecursively(string filePath)
        {
            var directoryPath = System.IO.Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(filePath))
            {
                using (File.Create(filePath))
                {} //make sure the file get closed after use
            }
        }
    }
}
