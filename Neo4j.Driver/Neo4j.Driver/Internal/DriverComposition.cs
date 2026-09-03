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

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Neo4j.Driver.Internal;

internal interface IDriverComposition
{
    IPropertyEncryption PropertyEncryption();
}

internal partial class DriverComposition : IDriverComposition
{
    // ReSharper disable once UnusedMember.Local
    private static void Setup()
    {
        DI.Setup(nameof(DriverComposition))
            .Hint(Hint.Resolve, "Off")
            .Arg<DriverContext>("context")

            .Bind<IDateTimeProvider>().To(_ => DateTimeProvider.Instance)

            .Bind<IEnumerable<IInternalEncryptionProfile>>()
            .To(
                ctx =>
                {
                    ctx.Inject<DriverContext>(out var driverContext);
                    return driverContext.Config.Preview_PropertyEncryptionProfiles
                        .Cast<IInternalEncryptionProfile>();
                })

            .Bind<IAeadCipher>().As(Singleton).To<AesGcmCipher>()
            .Bind<IAliasToKeyIdCache>().As(Singleton).To<AliasToKeyIdCache>()
            .Bind<IBaselineCompatibilityGuard>().As(Singleton).To<BaselineCompatibilityGuard>()
            .Bind<ICryptoRandomProvider>().As(Singleton).To<CryptoRandomProvider>()
            .Bind<IEncapsulatedKeyManagerFactory>().As(Singleton).To<EncapsulatedKeyManagerFactory>()
            .Bind<IEncapsulatedKeyManagerProvider>().As(Singleton).To<EnvelopeEncapsulatedKeyManagerProvider>()
            .Bind<IEncryptedStructureCodec>().As(Singleton).To<EncryptedStructureCodec>()
            .Bind<IEncryptedValueBytesCodec>().As(Singleton).To<EncryptedValueBytesCodec>()
            .Bind<IEncryptionEngine>().As(Singleton).To<EnvelopeEncryptionEngine>()
            .Bind<IEncryptionEngineDispatcher>().As(Singleton).To<EncryptionEngineDispatcher>()
            .Bind<IEncryptionErrorPolicy>().As(Singleton).To<EncryptionErrorPolicy>()
            .Bind<IEncryptionKeyCache>().As(Singleton).To<EncryptionKeyCache>()
            .Bind<IEncryptionProfileRegistry>().As(Singleton).To<EncryptionProfileRegistry>()
            .Bind<IEncryptionRequestRunner>().As(Singleton).To<EncryptionRequestRunner>()
            .Bind<IEnvelopeDataKeyProvider>().As(Singleton).To<EnvelopeDataKeyProvider>()
            .Bind<IEnvelopeMetadataBuilder>().As(Singleton).To<EnvelopeMetadataBuilder>()
            .Bind<IEnvelopeMetadataExtractor>().As(Singleton).To<EnvelopeMetadataExtractor>()
            .Bind<IIvProvider>().As(Singleton).To<IvProvider>()
            .Bind<IKeyDerivation>().As(Singleton).To<HkdfKeyDerivation>()
            .Bind<IMessageFormatFactory>().As(Singleton).To<MessageFormatFactory>()
            .Bind<IPackStreamMemorySerializer>().As(Singleton).To<PackStreamMemorySerializer>()
            .Bind<IPackStreamReaderWriterFactory>().As(Singleton).To<PackStreamReaderWriterFactory>()
            .Bind<IPlaintextCodec>().As(Singleton).To<PlaintextCodec>()
            .Bind<IPropertyTypeInspector>().As(Singleton).To<PropertyTypeInspector>()

            .Bind<IPropertyEncryption>().As(Singleton).To<PropertyEncryption>()
            .Root<IPropertyEncryption>("PropertyEncryption", kind: RootKinds.Public | RootKinds.Method);
    }
}
