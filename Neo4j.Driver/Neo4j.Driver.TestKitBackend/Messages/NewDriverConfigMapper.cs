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

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface INewDriverConfigMapper
{
    void Apply(NewDriverRequest request, IConfigBuilder builder);
}

internal class NewDriverConfigMapper : INewDriverConfigMapper
{
    private static readonly HashSet<string> HandledExplicitly =
    [
        nameof(NewDriverRequest.MaxTxRetryTimeMs),
        nameof(NewDriverRequest.LivenessCheckTimeoutMs),
        nameof(NewDriverRequest.Encrypted),
        nameof(NewDriverRequest.TelemetryDisabled),
        nameof(NewDriverRequest.TrustedCertificates),
        nameof(NewDriverRequest.NotificationsMinSeverity),
        nameof(NewDriverRequest.NotificationsDisabledCategories),
        nameof(NewDriverRequest.ClientCertificate),
        nameof(NewDriverRequest.ClientCertificateProviderId),
        nameof(NewDriverRequest.ResolverRegistered),
        nameof(NewDriverRequest.Uri),
        nameof(NewDriverRequest.AuthorizationToken),
        nameof(NewDriverRequest.AuthTokenManagerId),
        nameof(NewDriverRequest.DomainNameResolverRegistered)
    ];

    private readonly ICertificateLoader _certificateLoader;
    private readonly IRegistry _registry;
    private readonly IServerAddressResolver _resolver;
    private readonly IConfiguration _configuration;

    public NewDriverConfigMapper(
        ICertificateLoader certificateLoader,
        IRegistry registry,
        IServerAddressResolver resolver,
        IConfiguration configuration)
    {
        _certificateLoader = certificateLoader;
        _registry = registry;
        _resolver = resolver;
        _configuration = configuration;
    }

    public void Apply(NewDriverRequest request, IConfigBuilder builder)
    {
        ApplyResolver(request, builder);
        ApplyMaxTransactionRetryTime(request, builder);
        ApplyConnectionLivenessCheckTimeout(request, builder);
        ApplyEncryption(request, builder);
        ApplyTelemetry(request, builder);
        ApplyTrustedCertificates(request, builder);
        ApplyNotifications(request, builder);
        ApplyClientCertificate(request, builder);
        ApplyRemainingProperties(request, builder);
    }

    private void ApplyResolver(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.ResolverRegistered)
        {
            builder.WithResolver(_resolver);
        }
    }

    private void ApplyClientCertificate(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.ClientCertificate is { Value: var certificate })
        {
            builder.WithClientCertificateProvider(
                ClientCertificateProviders.Static(
                    _certificateLoader.Load(certificate.Certfile, certificate.Keyfile, certificate.Password)));
        }

        if (request.ClientCertificateProviderId is { } providerId)
        {
            builder.WithClientCertificateProvider(_registry.Get<IClientCertificateProvider>(providerId).Object);
        }
    }

    private static void ApplyMaxTransactionRetryTime(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.MaxTxRetryTimeMs is { } ms)
        {
            builder.WithMaxTransactionRetryTime(TimeSpan.FromMilliseconds(ms));
        }
    }

    private static void ApplyConnectionLivenessCheckTimeout(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.LivenessCheckTimeoutMs is { } ms)
        {
            builder.WithConnectionLivenessCheckTimeout(TimeSpan.FromMilliseconds(ms));
        }
    }

    private static void ApplyEncryption(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.Encrypted is { } encrypted)
        {
            builder.WithEncryptionLevel(encrypted ? EncryptionLevel.Encrypted : EncryptionLevel.None);
        }
    }

    private static void ApplyTelemetry(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.TelemetryDisabled == true)
        {
            builder.WithTelemetryDisabled();
        }
    }

    private void ApplyTrustedCertificates(NewDriverRequest request, IConfigBuilder builder)
    {
        if (!request.TrustedCertificates.IsSpecified(out var certificates))
        {
            return;
        }

        var rule = certificates switch
        {
            null => CertificateTrustRule.TrustSystem,
            { Length: 0 } => CertificateTrustRule.TrustAny,
            _ => CertificateTrustRule.TrustList
        };

        builder.WithCertificateTrustRule(
            rule,
            rule == CertificateTrustRule.TrustList 
                ? certificates!.Select(PrefixCaPath).ToList() 
                : null);
    }

    private string PrefixCaPath(string certificateFileName)
    {
        var caPath = _configuration["TK_CUSTOM_CA_PATH"] ??
            throw new InvalidOperationException(
                "trustedCertificates names a custom CA but TK_CUSTOM_CA_PATH is not configured.");

        return $"{caPath}{certificateFileName}";
    }

    private static void ApplyNotifications(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.NotificationsMinSeverity is null && request.NotificationsDisabledCategories is null)
        {
            return;
        }

        if (request.NotificationsMinSeverity == "OFF")
        {
            builder.WithNotificationsDisabled();
            return;
        }

        var severity = request.NotificationsMinSeverity is { } minSeverity
            ? Enum.Parse<Severity>(minSeverity, true)
            : (Severity?)null;

        var categories = request.NotificationsDisabledCategories
            ?.Select(c => Enum.Parse<Category>(c, true))
            .ToArray();

        builder.WithNotifications(severity, categories);
    }

    private static void ApplyRemainingProperties(NewDriverRequest request, IConfigBuilder builder)
    {
        foreach (var property in request.GetType().GetProperties())
        {
            if (HandledExplicitly.Contains(property.Name))
            {
                continue;
            }

            var value = property.GetValue(request);
            if (value is null)
            {
                continue;
            }

            var (methodName, argument) = property.Name.EndsWith("Ms", StringComparison.Ordinal)
                ? ("With" + property.Name[..^2], (object)TimeSpan.FromMilliseconds((long)value))
                : ("With" + property.Name, value);

            var method = typeof(IConfigBuilder).GetMethod(methodName) ??
                throw new InvalidOperationException(
                    $"No {methodName} method found on {nameof(IConfigBuilder)} for {property.Name}.");

            try
            {
                method.Invoke(builder, [argument]);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                throw e.InnerException;
            }
        }
    }
}
