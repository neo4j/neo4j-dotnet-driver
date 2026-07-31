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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewDriverRequest : IProtocolMessage
{
    public string Uri { get; init; } = "";
    public IWireType<AuthorizationToken>? AuthorizationToken { get; init; }
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
    public IWireType<ClientCertificate>? ClientCertificate { get; init; }
    public string? ClientCertificateProviderId { get; init; }
    public string? NotificationsMinSeverity { get; init; }
    public string[]? NotificationsDisabledCategories { get; init; }
    public bool? TelemetryDisabled { get; init; }
    public bool? DisableAutoCommitRetries { get; init; }
    public bool? Encrypted { get; init; }
    public Optional<string[]?> TrustedCertificates { get; init; }
}

internal record DriverResponse(string Id) : IProtocolMessage;

internal class NewDriverHandler : MessageHandler<NewDriverRequest>
{
    private readonly IRegistry _registry;
    private readonly INewDriverConfigMapper _configMapper;
    private readonly INeo4jLogger _neo4JLogger;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewDriverHandler(
        IRegistry registry,
        INewDriverConfigMapper configMapper,
        INeo4jLogger neo4JLogger,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _configMapper = configMapper;
        _neo4JLogger = neo4JLogger;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewDriverRequest message)
    {
        void Configure(ConfigBuilder builder)
        {
            _configMapper.Apply(message, builder);
            builder.WithLogger(_neo4JLogger);
        }

        var driver = message.AuthTokenManagerId is not null
            ? GraphDatabase.Driver(
                message.Uri,
                _registry.Get<IAuthTokenManager>(message.AuthTokenManagerId).Object,
                Configure)
            : GraphDatabase.Driver(message.Uri, message.AuthorizationToken?.Value.ToAuthToken(), Configure);

        var registryObject = _registry.Register(driver);
        _logger.LogDebug("Created driver with id '{Id}'", registryObject.Id);
        await _responseWriter.WriteAsync(new DriverResponse(registryObject.Id));
    }
}
