﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal.Services;

namespace Neo4j.Driver.Internal.Auth;

internal class ExpirationBasedAuthTokenManager : IAuthTokenManager
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IExpiringAuthTokenProvider _expiringAuthTokenProvider;
    private AuthTokenAndExpiration _currentAuthTokenAndExpiration;
    private readonly SemaphoreSlim _sync;

    public ExpirationBasedAuthTokenManager(IExpiringAuthTokenProvider expiringAuthTokenProvider)
        : this(DateTimeProvider.Instance, expiringAuthTokenProvider)
    {
    }

    internal ExpirationBasedAuthTokenManager(
        IDateTimeProvider dateTimeProvider,
        IExpiringAuthTokenProvider expiringAuthTokenProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _expiringAuthTokenProvider = expiringAuthTokenProvider;
        _sync = new SemaphoreSlim(1);
    }

    /// <inheritdoc/>
    public async Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_currentAuthTokenAndExpiration is not null &&
                _currentAuthTokenAndExpiration.Expiry > _dateTimeProvider.Now())
            {
                return _currentAuthTokenAndExpiration.Token;
            }

            _currentAuthTokenAndExpiration = await _expiringAuthTokenProvider.GetTokenAsync()!.ConfigureAwait(false);
            return _currentAuthTokenAndExpiration.Token;
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc/>
    public async Task OnTokenExpiredAsync(IAuthToken token, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_currentAuthTokenAndExpiration?.Token != null && Equals(token, _currentAuthTokenAndExpiration?.Token))
            {
                _currentAuthTokenAndExpiration =
                    await _expiringAuthTokenProvider.GetTokenAsync()!.ConfigureAwait(false);
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}
