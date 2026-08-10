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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewDriverConfigMapperTests
{
    private readonly AutoMocker _autoMocker;
    private readonly Mock<IConfigBuilder> _builder;

    public NewDriverConfigMapperTests()
    {
        _autoMocker = AutoMocker.ForTesting<NewDriverConfigMapper>();
        _builder = _autoMocker.GetMock<IConfigBuilder>();
    }

    private static NewDriverRequest MinimalRequest()
    {
        return new NewDriverRequest { Uri = "bolt://x" };
    }

    private void Apply(NewDriverRequest request)
    {
        var mapper = _autoMocker.CreateInstance<NewDriverConfigMapper>();
        mapper.Apply(request, _builder.Object);
    }

    [Fact]
    public void Calls_nothing_when_no_recognised_field_is_set()
    {
        var request = MinimalRequest() with
        {
            ResolverRegistered = false,
            DomainNameResolverRegistered = false
        };

        Apply(request);

        _builder.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Maps_userAgent_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { UserAgent = "custom-agent" });

        _builder.Verify(b => b.WithUserAgent("custom-agent"), Times.Once);
    }

    [Fact]
    public void Maps_maxConnectionPoolSize_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { MaxConnectionPoolSize = 42 });

        _builder.Verify(b => b.WithMaxConnectionPoolSize(42), Times.Once);
    }

    [Fact]
    public void Maps_fetchSize_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { FetchSize = 1000 });

        _builder.Verify(b => b.WithFetchSize(1000), Times.Once);
    }

    [Fact]
    public void Maps_disableAutoCommitRetries_via_the_fallback_tier()
    {
        Apply(MinimalRequest() with { DisableAutoCommitRetries = true });

        _builder.Verify(b => b.WithDisableAutoCommitRetries(true), Times.Once);
    }

    [Fact]
    public void Maps_connectionTimeoutMs_via_the_Ms_convention_tier()
    {
        Apply(MinimalRequest() with { ConnectionTimeoutMs = 5000 });

        _builder.Verify(b => b.WithConnectionTimeout(TimeSpan.FromMilliseconds(5000)), Times.Once);
    }

    [Fact]
    public void Maps_connectionAcquisitionTimeoutMs_via_the_Ms_convention_tier()
    {
        Apply(MinimalRequest() with { ConnectionAcquisitionTimeoutMs = 60000 });

        _builder.Verify(b => b.WithConnectionAcquisitionTimeout(TimeSpan.FromMilliseconds(60000)), Times.Once);
    }

    [Fact]
    public void Maps_maxConnectionLifetimeMs_via_the_Ms_convention_tier()
    {
        Apply(MinimalRequest() with { MaxConnectionLifetimeMs = 3600000 });

        _builder.Verify(b => b.WithMaxConnectionLifetime(TimeSpan.FromMilliseconds(3600000)), Times.Once);
    }

    [Fact]
    public void Maps_maxTxRetryTimeMs_to_WithMaxTransactionRetryTime()
    {
        Apply(MinimalRequest() with { MaxTxRetryTimeMs = 30000 });

        _builder.Verify(b => b.WithMaxTransactionRetryTime(TimeSpan.FromMilliseconds(30000)), Times.Once);
    }

    [Fact]
    public void Maps_livenessCheckTimeoutMs_to_WithConnectionLivenessCheckTimeout()
    {
        Apply(MinimalRequest() with { LivenessCheckTimeoutMs = 15000 });

        _builder.Verify(b => b.WithConnectionLivenessCheckTimeout(TimeSpan.FromMilliseconds(15000)), Times.Once);
    }

    [Fact]
    public void Maps_encrypted_true_to_the_Encrypted_level()
    {
        Apply(MinimalRequest() with { Encrypted = true });

        _builder.Verify(b => b.WithEncryptionLevel(EncryptionLevel.Encrypted), Times.Once);
    }

    [Fact]
    public void Maps_encrypted_false_to_the_None_level()
    {
        Apply(MinimalRequest() with { Encrypted = false });

        _builder.Verify(b => b.WithEncryptionLevel(EncryptionLevel.None), Times.Once);
    }

    [Fact]
    public void Leaves_encryption_level_unset_when_encrypted_is_absent()
    {
        Apply(MinimalRequest() with { Encrypted = null });

        _builder.Verify(b => b.WithEncryptionLevel(It.IsAny<EncryptionLevel>()), Times.Never);
    }

    [Fact]
    public void Disables_telemetry_when_telemetryDisabled_is_true()
    {
        Apply(MinimalRequest() with { TelemetryDisabled = true });

        _builder.Verify(b => b.WithTelemetryDisabled(), Times.Once);
    }

    [Fact]
    public void Leaves_telemetry_enabled_when_telemetryDisabled_is_false()
    {
        Apply(MinimalRequest() with { TelemetryDisabled = false });

        _builder.Verify(b => b.WithTelemetryDisabled(), Times.Never);
    }

    [Fact]
    public void Leaves_trust_rule_unset_when_trustedCertificates_is_absent()
    {
        Apply(MinimalRequest() with { TrustedCertificates = Optional<string[]?>.Absent });

        _builder.Verify(
            b => b.WithCertificateTrustRule(It.IsAny<CertificateTrustRule>(), It.IsAny<IReadOnlyList<string>>()),
            Times.Never);
    }

    [Fact]
    public void Maps_a_present_null_trustedCertificates_to_system_trust()
    {
        Apply(MinimalRequest() with { TrustedCertificates = Optional<string[]?>.Specified(null) });

        _builder.Verify(b => b.WithCertificateTrustRule(CertificateTrustRule.TrustSystem, null), Times.Once);
    }

    [Fact]
    public void Maps_an_empty_trustedCertificates_list_to_trust_any()
    {
        Apply(MinimalRequest() with { TrustedCertificates = Optional<string[]?>.Specified([]) });

        _builder.Verify(b => b.WithCertificateTrustRule(CertificateTrustRule.TrustAny, null), Times.Once);
    }

    [Fact]
    public void Maps_trustedCertificates_paths_to_a_trust_list_prefixed_with_the_configured_CA_path()
    {
        _autoMocker.GetMock<IConfiguration>()
            .Setup(c => c["TK_CUSTOM_CA_PATH"])
            .Returns("/certs/");

        Apply(MinimalRequest() with
        {
            TrustedCertificates = Optional<string[]?>.Specified(["customRoot.crt", "customRoot2.crt"])
        });

        _builder.Verify(
            b => b.WithCertificateTrustRule(
                CertificateTrustRule.TrustList,
                It.Is<IReadOnlyList<string>>(
                    paths => paths.SequenceEqual(new[] { "/certs/customRoot.crt", "/certs/customRoot2.crt" }))),
            Times.Once);
    }

    [Fact]
    public void Throws_when_a_trust_list_is_requested_but_no_CA_path_is_configured()
    {
        var act = () => Apply(MinimalRequest() with
        {
            TrustedCertificates = Optional<string[]?>.Specified(["customRoot.crt"])
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*TK_CUSTOM_CA_PATH*");
    }

    [Fact]
    public void Disables_notifications_when_minimum_severity_is_OFF()
    {
        Apply(MinimalRequest() with { NotificationsMinSeverity = "OFF" });

        _builder.Verify(b => b.WithNotificationsDisabled(), Times.Once);
    }

    [Fact]
    public void Maps_notification_severity_and_categories_to_one_WithNotifications_call()
    {
        Apply(
            MinimalRequest() with
            {
                NotificationsMinSeverity = "WARNING",
                NotificationsDisabledCategories = ["HINT", "GENERIC"]
            });

        _builder.Verify(
            b => b.WithNotifications(
                Severity.Warning,
                It.Is<Category[]>(c => c.SequenceEqual(new[] { Category.Hint, Category.Generic }))),
            Times.Once);
    }

    [Fact]
    public void Maps_notification_categories_alone_when_severity_is_absent()
    {
        Apply(MinimalRequest() with { NotificationsDisabledCategories = ["SECURITY"] });

        _builder.Verify(
            b => b.WithNotifications(null, It.Is<Category[]>(c => c.SequenceEqual(new[] { Category.Security }))),
            Times.Once);
    }

    [Fact]
    public void Leaves_notifications_unset_when_both_fields_are_absent()
    {
        Apply(MinimalRequest());

        _builder.Verify(b => b.WithNotificationsDisabled(), Times.Never);
        _builder.Verify(
            b => b.WithNotifications(It.IsAny<Severity?>(), It.IsAny<Category[]>()),
            Times.Never);
    }

    [Fact]
    public async Task Maps_the_client_certificate_to_a_static_provider_of_the_loaded_certificate()
    {
        using var key = RSA.Create();
        var request = new CertificateRequest("CN=mapper-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        _autoMocker.GetMock<ICertificateLoader>()
            .Setup(l => l.Load("cert.pem", "key.pem", "secret"))
            .Returns(certificate);

        IClientCertificateProvider? provider = null;
        _builder
            .Setup(b => b.WithClientCertificateProvider(It.IsAny<IClientCertificateProvider>()))
            .Callback<IClientCertificateProvider>(p => provider = p);

        Apply(MinimalRequest() with
        {
            ClientCertificate = new ClientCertificate("cert.pem", "key.pem", "secret")
        });

        provider.Should().NotBeNull();
        (await provider!.GetCertificateAsync()).Should().BeSameAs(certificate);
    }

    [Fact]
    public void Maps_clientCertificateProviderId_to_the_registered_provider()
    {
        var provider = Mock.Of<IClientCertificateProvider>();
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Get<IClientCertificateProvider>("provider-1"))
            .Returns(new RegistryObject<IClientCertificateProvider>("provider-1", provider));

        Apply(MinimalRequest() with { ClientCertificateProviderId = "provider-1" });

        _builder.Verify(b => b.WithClientCertificateProvider(provider), Times.Once);
    }

    [Fact]
    public void Maps_resolverRegistered_to_the_injected_resolver()
    {
        Apply(MinimalRequest() with { ResolverRegistered = true });

        _builder.Verify(b => b.WithResolver(_autoMocker.Get<IServerAddressResolver>()), Times.Once);
    }

    [Fact]
    public void Throws_when_a_fallback_tier_property_has_no_matching_builder_method()
    {
        var request = new RequestWithUnmappedProperty { Uri = "bolt://x", Nonexistent = "value" };

        var act = () => Apply(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Nonexistent*");
    }

    [Fact]
    public void Surfaces_the_builders_own_exception_instead_of_a_TargetInvocationException()
    {
        _builder.Setup(b => b.WithFetchSize(-5)).Throws(new ArgumentOutOfRangeException("size", "boom"));

        var act = () => Apply(MinimalRequest() with { FetchSize = -5 });

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*boom*");
    }

    private record RequestWithUnmappedProperty : NewDriverRequest
    {
        public string? Nonexistent { get; init; }
    }
}
