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

using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewDriverRequestTests
{
    private static IMessageSerializer Serializer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<BackendModule>();
        builder.RegisterInstance(new TestOutputLoggerFactory()).As<ILoggerFactory>();
        return builder.Build().BeginLifetimeScope().Resolve<IMessageSerializer>();
    }

    [Fact]
    public void Deserializes_the_payload_testkit_sends_for_a_minimal_driver()
    {
        const string json =
            """
            {"name": "NewDriver", "data": {"uri": "bolt://127.0.0.1:9010", "authorizationToken":
            {"name": "AuthorizationToken", "data": {"scheme": "basic", "principal": "",
            "credentials": ""}}, "authTokenManagerId": null, "userAgent": null,
            "resolverRegistered": false, "domainNameResolverRegistered": false,
            "connectionTimeoutMs": null, "fetchSize": null, "maxTxRetryTimeMs": null,
            "livenessCheckTimeoutMs": null, "maxConnectionPoolSize": null,
            "connectionAcquisitionTimeoutMs": null, "clientCertificate": null,
            "clientCertificateProviderId": null}}
            """;

        var message = Serializer().Deserialize(json);

        message.Should().BeOfType<NewDriverRequest>();
        var request = (NewDriverRequest)message;
        request.Uri.Should().Be("bolt://127.0.0.1:9010");
        request.AuthorizationToken.Should().Be(new AuthorizationToken
        {
            Scheme = "basic",
            Principal = "",
            Credentials = ""
        });
    }

    [Fact]
    public void Deserializes_a_payload_with_every_field_populated()
    {
        const string json =
            """
            {"name": "NewDriver", "data": {"uri": "neo4j://localhost:7687", "authorizationToken":
            {"name": "AuthorizationToken", "data": {"scheme": "basic", "principal": "neo4j",
            "credentials": "secret", "realm": "myrealm"}}, "authTokenManagerId": null,
            "userAgent": "custom-agent", "resolverRegistered": true,
            "domainNameResolverRegistered": true, "connectionTimeoutMs": 5000,
            "fetchSize": 1000, "maxTxRetryTimeMs": 30000, "livenessCheckTimeoutMs": 15000,
            "maxConnectionPoolSize": 100, "connectionAcquisitionTimeoutMs": 60000,
            "maxConnectionLifetimeMs": 3600000, "clientCertificate":
            {"name": "ClientCertificate", "data": {"certfile": "cert.pem", "keyfile": "key.pem",
            "password": "pw"}}, "clientCertificateProviderId": null,
            "notificationsMinSeverity": "WARNING",
            "notificationsDisabledCategories": ["HINT", "GENERIC"], "telemetryDisabled": true,
            "disableAutoCommitRetries": true, "encrypted": true,
            "trustedCertificates": ["customRoot.crt"]}}
            """;

        var message = Serializer().Deserialize(json);

        message.Should().BeOfType<NewDriverRequest>();
        var request = (NewDriverRequest)message;
        request.Uri.Should().Be("neo4j://localhost:7687");
        request.AuthorizationToken.Should().Be(new AuthorizationToken
        {
            Scheme = "basic",
            Principal = "neo4j",
            Credentials = "secret",
            Realm = "myrealm"
        });
        request.AuthTokenManagerId.Should().BeNull();
        request.UserAgent.Should().Be("custom-agent");
        request.ResolverRegistered.Should().BeTrue();
        request.DomainNameResolverRegistered.Should().BeTrue();
        request.ConnectionTimeoutMs.Should().Be(5000);
        request.FetchSize.Should().Be(1000);
        request.MaxTxRetryTimeMs.Should().Be(30000);
        request.LivenessCheckTimeoutMs.Should().Be(15000);
        request.MaxConnectionPoolSize.Should().Be(100);
        request.ConnectionAcquisitionTimeoutMs.Should().Be(60000);
        request.MaxConnectionLifetimeMs.Should().Be(3600000);
        request.ClientCertificate.Should().Be(new ClientCertificate("cert.pem", "key.pem", "pw"));
        request.ClientCertificateProviderId.Should().BeNull();
        request.NotificationsMinSeverity.Should().Be("WARNING");
        request.NotificationsDisabledCategories.Should().Equal("HINT", "GENERIC");
        request.TelemetryDisabled.Should().BeTrue();
        request.DisableAutoCommitRetries.Should().BeTrue();
        request.Encrypted.Should().BeTrue();
        request.TrustedCertificates.IsSpecified(out var certs).Should().BeTrue();
        certs.Should().Equal("customRoot.crt");
    }

    [Fact]
    public void Deserializes_the_property_encryption_profiles()
    {
        const string json =
            """
            {"name": "NewDriver", "data": {"uri": "bolt://x", "propertyEncryptionProfiles":
            [{"name": "profile-a", "kek": "0102030405060708"}, {"name": "profile-b", "kek": null}]}}
            """;

        var request = (NewDriverRequest)Serializer().Deserialize(json);

        request.PropertyEncryptionProfiles.Should().Equal(
            new PropertyEncryptionProfileInput("profile-a", new HexBytes([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08])),
            new PropertyEncryptionProfileInput("profile-b", null));
    }

    [Fact]
    public void Distinguishes_an_absent_trustedCertificates_from_a_present_null()
    {
        const string absentJson =
            """{"name": "NewDriver", "data": {"uri": "bolt://x"}}""";
        const string nullJson =
            """{"name": "NewDriver", "data": {"uri": "bolt://x", "trustedCertificates": null}}""";

        var absent = (NewDriverRequest)Serializer().Deserialize(absentJson);
        var withNull = (NewDriverRequest)Serializer().Deserialize(nullJson);

        absent.TrustedCertificates.IsSpecified(out _).Should().BeFalse();
        withNull.TrustedCertificates.IsSpecified(out var certs).Should().BeTrue();
        certs.Should().BeNull();
    }
}
