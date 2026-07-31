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

using Neo4j.Driver.TestKitBackend.Certificates;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface INewDriverConfigMapper
{
    void Apply(NewDriverRequest request, IConfigBuilder builder);
}

internal class NewDriverConfigMapper : INewDriverConfigMapper
{
    // Properties applied by the special cases below - excluded from the generic tiers so they're
    // never also (mis)matched by name against IConfigBuilder there.
    private static readonly HashSet<string> HandledExplicitly =
    [
        nameof(NewDriverRequest.MaxTxRetryTimeMs),
        nameof(NewDriverRequest.LivenessCheckTimeoutMs),
        nameof(NewDriverRequest.Encrypted),
        nameof(NewDriverRequest.TelemetryDisabled),
        nameof(NewDriverRequest.TrustedCertificates),
        nameof(NewDriverRequest.NotificationsMinSeverity),
        nameof(NewDriverRequest.NotificationsDisabledCategories),
        nameof(NewDriverRequest.ClientCertificate)
    ];

    private readonly ICertificateLoader _certificateLoader;

    public NewDriverConfigMapper(ICertificateLoader certificateLoader)
    {
        _certificateLoader = certificateLoader;
    }

    public void Apply(NewDriverRequest request, IConfigBuilder builder)
    {
        ApplyMaxTransactionRetryTime(request, builder);
        ApplyConnectionLivenessCheckTimeout(request, builder);
        ApplyEncryption(request, builder);
        ApplyTelemetry(request, builder);
        ApplyTrustedCertificates(request, builder);
        ApplyNotifications(request, builder);
        ApplyClientCertificate(request, builder);
        ApplyRemainingProperties(request, builder);
    }

    private void ApplyClientCertificate(NewDriverRequest request, IConfigBuilder builder)
    {
        if (request.ClientCertificate is { Value: var certificate })
        {
            builder.WithClientCertificateProvider(
                ClientCertificateProviders.Static(
                    _certificateLoader.Load(certificate.Certfile, certificate.Keyfile, certificate.Password)));
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

    private static void ApplyTrustedCertificates(NewDriverRequest request, IConfigBuilder builder)
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

        builder.WithCertificateTrustRule(rule, rule == CertificateTrustRule.TrustList ? certificates : null);
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

    // The remaining fields are either a direct name match (With{PropertyName}) or follow the *Ms
    // convention (strip "Ms", convert to a TimeSpan, With{Stripped}). If IConfigBuilder has no
    // matching method, the field is silently skipped - it's either not driver config (e.g. Uri) or
    // not wired up yet (e.g. resolverRegistered).
    private static void ApplyRemainingProperties(NewDriverRequest request, IConfigBuilder builder)
    {
        foreach (var property in typeof(NewDriverRequest).GetProperties())
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

            var method = typeof(IConfigBuilder).GetMethod(methodName);
            method?.Invoke(builder, [argument]);
        }
    }
}
