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
using Neo4j.Driver.TestKitBackend.Protocol;
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
}

internal record DriverResponse(string Id) : IProtocolMessage;

internal class NewDriverHandler : MessageHandler<NewDriverRequest>
{
    private readonly IRegistry _registry;
    private readonly ILogger _logger;

    public NewDriverHandler(
        IRegistry registry,
        ILogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public override Task<IProtocolMessage?> ProcessAsync(NewDriverRequest message)
    {
        var driver = GraphDatabase.Driver(message.Uri, message.AuthorizationToken?.ToAuthToken());
        var registryObject = _registry.Register(driver);
        var response = new DriverResponse(registryObject.Id);
        _logger.LogDebug("Created driver with id '{Id}'", registryObject.Id);
        return Task.FromResult<IProtocolMessage?>(response);
    }
}
