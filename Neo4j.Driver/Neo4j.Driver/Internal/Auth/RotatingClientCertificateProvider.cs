﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Auth;

namespace Neo4j.Driver.Internal.Auth;

internal class RotatingClientCertificateProvider : IRotatingClientCertificateProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private X509Certificate _certificate;

    public RotatingClientCertificateProvider(X509Certificate certificate)
    {
        _certificate = certificate;
    }

    public ValueTask<X509Certificate> GetCertificateAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return new ValueTask<X509Certificate>(_certificate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void UpdateCertificate(X509Certificate certificate)
    {
        _lock.EnterWriteLock();
        try
        {
            _certificate = certificate;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
