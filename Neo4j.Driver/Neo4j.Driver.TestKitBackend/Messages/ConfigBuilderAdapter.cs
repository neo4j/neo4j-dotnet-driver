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

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface IConfigBuilder
{
    IConfigBuilder WithUserAgent(string userAgent);
    IConfigBuilder WithMaxConnectionPoolSize(int size);
    IConfigBuilder WithFetchSize(long size);
    IConfigBuilder WithDisableAutoCommitRetries(bool disable);
    IConfigBuilder WithConnectionTimeout(TimeSpan timeSpan);
    IConfigBuilder WithConnectionAcquisitionTimeout(TimeSpan timeSpan);
    IConfigBuilder WithMaxConnectionLifetime(TimeSpan timeSpan);
    IConfigBuilder WithMaxTransactionRetryTime(TimeSpan time);
    IConfigBuilder WithConnectionLivenessCheckTimeout(TimeSpan timeout);
    IConfigBuilder WithEncryptionLevel(EncryptionLevel level);
    IConfigBuilder WithTelemetryDisabled();

    IConfigBuilder WithCertificateTrustRule(
        CertificateTrustRule certificateTrustRule,
        IReadOnlyList<string>? trustedCaCertificateFileNames);

    IConfigBuilder WithNotificationsDisabled();
    IConfigBuilder WithNotifications(Severity? minimumSeverity, Category[]? disabledCategories);
    IConfigBuilder WithClientCertificateProvider(IClientCertificateProvider clientCertificateProvider);
    IConfigBuilder WithResolver(IServerAddressResolver resolver);
}

internal class ConfigBuilderAdapter : IConfigBuilder
{
    private readonly ConfigBuilder _builder;

    public ConfigBuilderAdapter(ConfigBuilder builder)
    {
        _builder = builder;
    }

    public IConfigBuilder WithUserAgent(string userAgent)
    {
        _builder.WithUserAgent(userAgent);
        return this;
    }

    public IConfigBuilder WithMaxConnectionPoolSize(int size)
    {
        _builder.WithMaxConnectionPoolSize(size);
        return this;
    }

    public IConfigBuilder WithFetchSize(long size)
    {
        _builder.WithFetchSize(size);
        return this;
    }

    public IConfigBuilder WithDisableAutoCommitRetries(bool disable)
    {
        _builder.WithDisableAutoCommitRetries(disable);
        return this;
    }

    public IConfigBuilder WithConnectionTimeout(TimeSpan timeSpan)
    {
        _builder.WithConnectionTimeout(timeSpan);
        return this;
    }

    public IConfigBuilder WithConnectionAcquisitionTimeout(TimeSpan timeSpan)
    {
        _builder.WithConnectionAcquisitionTimeout(timeSpan);
        return this;
    }

    public IConfigBuilder WithMaxConnectionLifetime(TimeSpan timeSpan)
    {
        _builder.WithMaxConnectionLifetime(timeSpan);
        return this;
    }

    public IConfigBuilder WithMaxTransactionRetryTime(TimeSpan time)
    {
        _builder.WithMaxTransactionRetryTime(time);
        return this;
    }

    public IConfigBuilder WithConnectionLivenessCheckTimeout(TimeSpan timeout)
    {
        _builder.WithConnectionLivenessCheckTimeout(timeout);
        return this;
    }

    public IConfigBuilder WithEncryptionLevel(EncryptionLevel level)
    {
        _builder.WithEncryptionLevel(level);
        return this;
    }

    public IConfigBuilder WithTelemetryDisabled()
    {
        _builder.WithTelemetryDisabled();
        return this;
    }

    public IConfigBuilder WithCertificateTrustRule(
        CertificateTrustRule certificateTrustRule,
        IReadOnlyList<string>? trustedCaCertificateFileNames)
    {
        _builder.WithCertificateTrustRule(certificateTrustRule, trustedCaCertificateFileNames);
        return this;
    }

    public IConfigBuilder WithNotificationsDisabled()
    {
        _builder.WithNotificationsDisabled();
        return this;
    }

    public IConfigBuilder WithNotifications(Severity? minimumSeverity, Category[]? disabledCategories)
    {
        _builder.WithNotifications(minimumSeverity, disabledCategories);
        return this;
    }

    public IConfigBuilder WithClientCertificateProvider(IClientCertificateProvider clientCertificateProvider)
    {
        _builder.WithClientCertificateProvider(clientCertificateProvider);
        return this;
    }

    public IConfigBuilder WithResolver(IServerAddressResolver resolver)
    {
        _builder.WithResolver(resolver);
        return this;
    }
}
