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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal.Auth;

internal class Neo4jAuthTokenManager : IAuthTokenManager
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Func<ValueTask<AuthTokenAndExpiration>> _getAuthTokenAndExpirationAsync;
    private readonly Type[] _handledExceptionTypes;
    private readonly SemaphoreSlim _sync;
    private AuthTokenAndExpiration _currentAuthTokenAndExpiration;

    public Neo4jAuthTokenManager(
        Func<ValueTask<AuthTokenAndExpiration>> getAuthTokenAndExpirationAsync,
        params Type[] handledExceptionTypes)
        : this(DateTimeProvider.Instance, getAuthTokenAndExpirationAsync, handledExceptionTypes)
    {
    }

    internal Neo4jAuthTokenManager(
        IDateTimeProvider dateTimeProvider,
        Func<ValueTask<AuthTokenAndExpiration>> getAuthTokenAndExpirationAsync,
        params Type[] handledExceptionTypes)
    {
        _dateTimeProvider = dateTimeProvider;
        _getAuthTokenAndExpirationAsync = getAuthTokenAndExpirationAsync;
        _handledExceptionTypes = handledExceptionTypes;
        _sync = new SemaphoreSlim(1);
    }

    /// <inheritdoc/>
    public async ValueTask<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_currentAuthTokenAndExpiration is not null &&
                _currentAuthTokenAndExpiration.Expiry > _dateTimeProvider.Now())
            {
                return _currentAuthTokenAndExpiration.Token;
            }

            _currentAuthTokenAndExpiration = await _getAuthTokenAndExpirationAsync()!.ConfigureAwait(false);
            return _currentAuthTokenAndExpiration.Token;
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default)
    {
        if (_handledExceptionTypes.Any(t => t.IsInstanceOfType(exception)))
        {
            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_currentAuthTokenAndExpiration?.Token != null &&
                    Equals(token, _currentAuthTokenAndExpiration?.Token))
                {
                    _currentAuthTokenAndExpiration =
                        await _getAuthTokenAndExpirationAsync()!.ConfigureAwait(false);
                }
            }
            finally
            {
                _sync.Release();
            }

            return true;
        }

        return false;
    }
}
