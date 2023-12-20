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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Helpers;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class SocketConnection : IConnection
{
    private readonly ISocketClient _client;
    private readonly string _idPrefix;

    private readonly ILogger _logger;

    private readonly Queue<IRequestMessage> _messages = new();
    private readonly IBoltProtocolFactory _protocolFactory;
    private readonly SemaphoreSlim _recvLock = new(1, 1);
    private readonly IResponsePipeline _responsePipeline;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ServerInfo _serverInfo;

    private string _id;

    internal SocketConnection(
        Uri uri,
        DriverContext context,
        IAuthToken authToken)
    {
        _idPrefix = $"conn-{uri.Host}:{uri.Port}-";
        _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
        _logger = context.Logger != NullLogger.Instance
            ? new PrefixLogger(context.Logger, FormatPrefix(_id))
            : context.Logger;

        _client = new SocketClient(uri, context, _logger, null);
        Context = context;
        AuthToken = authToken;
        _serverInfo = new ServerInfo(uri);

        _responsePipeline = new ResponsePipeline(_logger);
        AuthTokenManager = context.AuthTokenManager;
        _protocolFactory = BoltProtocolFactory.Default;
    }

    // for test only
    internal SocketConnection(
        ISocketClient socketClient,
        IAuthToken authToken,
        ILogger logger,
        ServerInfo server,
        IResponsePipeline responsePipeline = null,
        IAuthTokenManager authTokenManager = null,
        IBoltProtocolFactory protocolFactory = null, 
        DriverContext context = null)
    {
        _client = socketClient ?? throw new ArgumentNullException(nameof(socketClient));
        AuthToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
        _serverInfo = server ?? throw new ArgumentNullException(nameof(server));
        AuthTokenManager = authTokenManager;
        _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
        _logger = new PrefixLogger(logger, FormatPrefix(_id));
        _responsePipeline = responsePipeline ?? new ResponsePipeline(logger);
        _protocolFactory = protocolFactory ?? BoltProtocolFactory.Default;
        Context = context;
    }

    internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

    public AccessMode? Mode { get; private set; }

    public string Database { get; private set; }

    public IDictionary<string, string> RoutingContext => Context.RoutingContext;
    public BoltProtocolVersion Version => _client.Version;

    /// <summary>Internal Set used for tests.</summary>
    public IBoltProtocol BoltProtocol { get; internal set; }

    public AuthorizationStatus AuthorizationStatus { get; set; }

    public void ConfigureMode(AccessMode? mode)
    {
        Mode = mode;
    }

    public void Configure(string database, AccessMode? mode)
    {
        Mode = mode;
        Database = database;
    }

    public async Task InitAsync(
        SessionConfig sessionConfig = null,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
            BoltProtocol = _protocolFactory.ForVersion(Version);
        }
        finally
        {
            _sendLock.Release();
        }

        SessionConfig = sessionConfig;
        var authToken = sessionConfig?.AuthToken ?? AuthToken;
        if (!this.SupportsReAuth() && sessionConfig?.AuthToken != null)
        {
            // Not allowed.
            throw new ReauthException(true);
        }

        try
        {
            await BoltProtocol.AuthenticateAsync(this, Context.Config.UserAgent, authToken, Context.Config.NotificationsConfig)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleAuthErrorAsync(ex).ConfigureAwait(false);
            throw;
        }

        if (this.SupportsReAuth() || sessionConfig?.AuthToken == null)
        {
            AuthorizationStatus = AuthorizationStatus.FreshlyAuthenticated;
        }
        else
        {
            AuthorizationStatus = AuthorizationStatus.SessionToken;
        }
    }

    public IAuthTokenManager AuthTokenManager { get; }

    public Task ReAuthAsync(
        IAuthToken newAuthToken,
        bool force,
        CancellationToken cancellationToken = default)
    {
        if (!force && newAuthToken.Equals(AuthToken))
        {
            // if the token is the same, we don't need to reauthenticate.
            AuthorizationStatus = AuthorizationStatus.FreshlyAuthenticated;
            return Task.CompletedTask;
        }

        if (!this.SupportsReAuth())
        {
            // if we are attempting to reauthenticate on 5.0 or earlier, throw an exception.
            throw new ReauthException(false);
        }

        // Assume success, if Reauth fails we destroy the connection.
        AuthToken = newAuthToken;
        AuthorizationStatus = AuthorizationStatus.FreshlyAuthenticated;
        return BoltProtocol.ReAuthAsync(this, newAuthToken);
    }

    public ValueTask<bool> NotifySecurityExceptionAsync(SecurityException exception)
    {
        if (SessionConfig?.AuthToken != null)
        {
            return new ValueTask<bool>(false);
        }

        return AuthTokenManager.HandleSecurityExceptionAsync(AuthToken, exception);
    }

    public async Task SyncAsync()
    {
        await SendAsync().ConfigureAwait(false);
        await ReceiveAsync().ConfigureAwait(false);
    }

    public async Task SendAsync()
    {
        if (_messages.Count == 0)
            // nothing to send
        {
            return;
        }

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // send
            await _client.SendAsync(_messages).ConfigureAwait(false);

            _messages.Clear();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task ReceiveOneAsync()
    {
        await _recvLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_responsePipeline.HasNoPendingMessages)
            {
                return;
            }

            await _client.ReceiveOneAsync(_responsePipeline).ConfigureAwait(false);

            await HandleAuthErrorAsync(_responsePipeline).ConfigureAwait(false);
            _responsePipeline.AssertNoFailure();
        }
        finally
        {
            _recvLock.Release();
        }
    }

    public Task ResetAsync()
    {
        return BoltProtocol.ResetAsync(this);
    }

    public bool IsOpen => _client.IsOpen;
    public IServerInfo Server => _serverInfo;

    public bool UtcEncodedDateTime { get; private set; }
    public DriverContext Context { get; }
    public IAuthToken AuthToken { get; private set; }
    public bool TelemetryEnabled { get; set; }

    public void UpdateId(string newConnId)
    {
        _logger.Debug(
            "Connection '{0}' renamed to '{1}'. The new name identifies the connection uniquely both on the client side and the server side.",
            _id,
            newConnId);

        _id = newConnId;
        
        if (_logger is PrefixLogger logger)
        {
            logger.Prefix = FormatPrefix(_id);
        }
    }

    public void UpdateVersion(ServerVersion newVersion)
    {
        _serverInfo.Update(Version, newVersion.Agent);
    }

    public Task DestroyAsync()
    {
        return CloseAsync();
    }

    public async Task CloseAsync()
    {
        try
        {
            try
            {
                if (BoltProtocol != null)
                {
                    await BoltProtocol.LogoutAsync(this).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                //ignore
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Any(x => x is ObjectDisposedException))
            {
                //ignore.
            }
            catch (Exception e)
            {
                _logger.Debug($"Failed to logout user before closing connection due to error: {e.Message}");
            }

            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // only log the exception if failed to close connection
            _logger.Warn(e, "Failed to close connection properly.");
        }
    }

    public async Task EnqueueAsync(IRequestMessage message, IResponseHandler handler)
    {
        await _sendLock.WaitAsync().ConfigureAwait(false);

        try
        {
            _messages.Enqueue(message);
            _responsePipeline.Enqueue(handler);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void SetReadTimeoutInSeconds(int seconds)
    {
        _client.SetReadTimeoutInSeconds(seconds);
    }

    public void SetUseUtcEncodedDateTime()
    {
        UtcEncodedDateTime = true;
        _client.UseUtcEncoded();
    }

    public SessionConfig SessionConfig { get; set; }

    public async ValueTask ValidateCredsAsync()
    {
        var token = AuthToken;
        if (AuthorizationStatus == AuthorizationStatus.SecurityError)
        {
            if (!this.SupportsReAuth())
            {
                // This shouldn't happen, connections that don't support re-auth should be cleaned up by the pool.
                throw new ReauthException(false);
            }

            // Get a new token
            token = await AuthTokenManager.GetTokenAsync().ConfigureAwait(false);
            if (token is null)
            {
                throw new InvalidOperationException("Auth token manager returned a null token.");
            }
        }
        else if (SessionConfig?.AuthToken != null)
        {
            if (!this.SupportsReAuth())
            {
                throw new ReauthException(true);
            }

            // user switching
            token = SessionConfig.AuthToken;
        }
        else if (AuthorizationStatus == AuthorizationStatus.Pooled)
        {
            token = await AuthTokenManager.GetTokenAsync().ConfigureAwait(false);
            if (token is null)
            {
                throw new InvalidOperationException("Auth token manager returned a null token.");
            }
        }

        var authExpired = AuthorizationStatus == AuthorizationStatus.AuthorizationExpired;
        if (authExpired || !token.Equals(AuthToken))
        {
            // The token has changed, or the connection needs to re-authenticate but can use the same credentials.
            // In the latter case, we need to force re-authentication.
            await ReAuthAsync(token, authExpired).ConfigureAwait(false);
        }
    }

    public Task LoginAsync(string userAgent, IAuthToken authToken, INotificationsConfig notificationsConfig)
    {
        return BoltProtocol.AuthenticateAsync(this, userAgent, authToken, notificationsConfig);
    }

    public Task LogoutAsync()
    {
        return BoltProtocol.LogoutAsync(this);
    }

    public Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks)
    {
        return BoltProtocol.GetRoutingTableAsync(this, database, sessionConfig, bookmarks);
    }

    public Task<IResultCursor> RunInAutoCommitTransactionAsync(
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig)
    {
        return BoltProtocol.RunInAutoCommitTransactionAsync(this, autoCommitParams, notificationsConfig);
    }

    public Task BeginTransactionAsync(BeginTransactionParams beginParams)
    {
        return BoltProtocol.BeginTransactionAsync(this, beginParams);
    }

    public Task<IResultCursor> RunInExplicitTransactionAsync(Query query, bool reactive, long fetchSize,
        IInternalAsyncTransaction transaction)
    {
        return BoltProtocol.RunInExplicitTransactionAsync(this, query, reactive, fetchSize, transaction);
    }

    public Task CommitTransactionAsync(IBookmarksTracker bookmarksTracker)
    {
        return BoltProtocol.CommitTransactionAsync(this, bookmarksTracker);
    }

    public Task RollbackTransactionAsync()
    {
        return BoltProtocol.RollbackTransactionAsync(this);
    }

    private ValueTask HandleAuthErrorAsync(IResponsePipeline responsePipeline)
    {
        return responsePipeline.IsHealthy(out var error)
            ? new ValueTask()
            : HandleAuthErrorAsync(error);
    }

    private async ValueTask HandleAuthErrorAsync(Exception error)
    {
        if (error is SecurityException se)
        {
            if (!se.Notified)
            {
                if (await NotifySecurityExceptionAsync(se).ConfigureAwait(false))
                {
                    se.Retriable = true;
                }

                se.Notified = true;
            }
        }
    }

    private async Task ReceiveAsync()
    {
        await _recvLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_responsePipeline.HasNoPendingMessages)
            {
                return;
            }

            await _client.ReceiveAsync(_responsePipeline).ConfigureAwait(false);

            await HandleAuthErrorAsync(_responsePipeline).ConfigureAwait(false);
            _responsePipeline.AssertNoFailure();
        }
        finally
        {
            _recvLock.Release();
        }
    }

    public override string ToString()
    {
        return _id;
    }

    private static string FormatPrefix(string id)
    {
        return $"[{id}]";
    }
}

internal class ReauthException : UnsupportedFeatureException
{
    internal readonly bool IsUserSwitching;

    public ReauthException(bool isUserSwitching) : base(
        "Attempted to use reauthentication or user switching but the " +
        "server does not support it. Please upgrade to neo4j 5.6.0 or later.")
    {
        IsUserSwitching = isUserSwitching;
    }
}
