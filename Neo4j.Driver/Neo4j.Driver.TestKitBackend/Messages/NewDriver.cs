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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewDriverRequest : IProtocolMessage
{
    public string Uri { get; init; } = "";
    public AuthorizationToken? AuthorizationToken { get; init; }
    public string? AuthTokenManagerId { get; init; }
    public string? UserAgent { get; init; }
    public bool ResolverRegistered { get; init; }
    public bool DomainNameResolverRegistered { get; init; }
    public long? ConnectionTimeoutMs { get; init; }
    public long? FetchSize { get; init; }
    public long? MaxTxRetryTimeMs { get; init; }
    public long? LivenessCheckTimeoutMs { get; init; }
    public int? MaxConnectionPoolSize { get; init; }
    public long? ConnectionAcquisitionTimeoutMs { get; init; }
    public long? MaxConnectionLifetimeMs { get; init; }
    public ClientCertificate? ClientCertificate { get; init; }
    public string? ClientCertificateProviderId { get; init; }
    public string? NotificationsMinSeverity { get; init; }
    public string[]? NotificationsDisabledCategories { get; init; }
    public bool? TelemetryDisabled { get; init; }
    public bool? DisableAutoCommitRetries { get; init; }
    public bool? Encrypted { get; init; }
    public Optional<string[]?> TrustedCertificates { get; init; }
    public IReadOnlyList<PropertyEncryptionProfileInput>? PropertyEncryptionProfiles { get; init; }
}

internal record DriverResponse(string Id) : IProtocolMessage;

internal class NewDriverHandler : MessageHandler<NewDriverRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly INewDriverConfigMapper _configMapper;
    private readonly INeo4jLogger _neo4JLogger;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;
    private readonly IDriverEncryptionSetup _driverEncryptionSetup;
    private readonly IDriverEncryptionObjectStore _driverEncryptionObjectStore;

    public NewDriverHandler(
        IObjectStore objectStore,
        INewDriverConfigMapper configMapper,
        INeo4jLogger neo4JLogger,
        IResponseWriter responseWriter,
        ILogger logger,
        IDriverEncryptionSetup driverEncryptionSetup,
        IDriverEncryptionObjectStore driverEncryptionObjectStore)
    {
        _objectStore = objectStore;
        _configMapper = configMapper;
        _neo4JLogger = neo4JLogger;
        _responseWriter = responseWriter;
        _logger = logger;
        _driverEncryptionSetup = driverEncryptionSetup;
        _driverEncryptionObjectStore = driverEncryptionObjectStore;
    }

    public override async Task ProcessAsync(NewDriverRequest message)
    {
        DriverEncryptionObjects? encryptionSetup = null;
        var hasEncryptionProfiles = message.PropertyEncryptionProfiles is not null;
        if (hasEncryptionProfiles)
        {
            encryptionSetup = _driverEncryptionSetup.Prepare(message.PropertyEncryptionProfiles!);
        }

        IDriver driver;
        if (message.AuthTokenManagerId is not null)
        {
            var authTokenManager = _objectStore.Get<IAuthTokenManager>(message.AuthTokenManagerId);
            driver = GraphDatabase.Driver(message.Uri, authTokenManager, Configure);
        }
        else
        {
            driver = GraphDatabase.Driver(message.Uri, message.AuthorizationToken?.ToAuthToken(), Configure);
        }

        if (hasEncryptionProfiles)
        {
            _driverEncryptionObjectStore.StoreObjects(driver, encryptionSetup!);
        }

        var id = _objectStore.Store(driver);
        _logger.LogDebug("Created driver with id '{Id}'", id);
        await _responseWriter.WriteAsync(new DriverResponse(id));

        return;

        void Configure(ConfigBuilder builder)
        {
            _configMapper.Apply(message, new ConfigBuilderAdapter(builder));
            builder.WithLogger(_neo4JLogger);
            builder.WithMetricsEnabled(true);
            if (encryptionSetup is not null)
            {
                builder.WithPropertyEncryptionProfiles(encryptionSetup.Profiles);
                builder.WithServiceOverride<IIvProvider>(encryptionSetup.IvProvider);
            }
        }
    }
}
