// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.Internal.Auth;

internal interface IInternalX509CertificateLoader
{
    X509Certificate2 LoadCertificate(byte[] rawData);
    X509Certificate2 LoadCertificate(string filename);
    X509Certificate2 LoadCertificate(byte[] rawData, string password, X509KeyStorageFlags flags);
}

// this class is to hide away the conditional compilation for .NET >= 9
internal class InternalX509CertificateLoader : IInternalX509CertificateLoader
{
    public X509Certificate2 LoadCertificate(byte[] rawData)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(rawData);
#else
        return new X509Certificate2(rawData);
#endif
    }

    public X509Certificate2 LoadCertificate(byte[] rawData, string password, X509KeyStorageFlags flags)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(rawData, password, flags, Pkcs12LoaderLimits.Defaults);
#else
        return new X509Certificate2(rawData, password, flags);
#endif
    }
    

    public X509Certificate2 LoadCertificate(string filename)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificateFromFile(filename);
#else
        return new X509Certificate2(filename);
#endif
    }
}
