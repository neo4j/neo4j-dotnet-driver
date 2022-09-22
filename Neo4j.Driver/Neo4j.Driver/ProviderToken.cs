// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

internal class ProviderToken : IAuthToken
{
    private readonly SemaphoreSlim _cacheLock;
    private readonly Func<IAuthToken> _provider;
    
    public readonly bool CacheToken;
    public readonly ConnectionPoolEvictionPolicy EvictionPolicy;
    public readonly Func<IAuthToken> Provider;
    
    private IAuthToken cachedToken;

    public ProviderToken(Func<IAuthToken> provider, bool cacheToken, ConnectionPoolEvictionPolicy evictionPolicy)
    {
        CacheToken = cacheToken;
        if (CacheToken)
        {
            _cacheLock = new SemaphoreSlim(1, 1);
            Provider = CacheProvider;
        }
        else
        {
            Provider = _provider;
        }

        _provider = provider;
        EvictionPolicy = evictionPolicy;
    }

    private IAuthToken CacheProvider()
    {
        _cacheLock.Wait();
        try
        {
            if (cachedToken == null)
                cachedToken = _provider();

            return cachedToken;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task UpdateTokenAsync()
    {
        if (!CacheToken)
            return;

        var read = cachedToken;
        await _cacheLock.WaitAsync().ConfigureAwait(false);

        try
        {
            // Check if another thread has updated the value while we awaited the lock.
            if (cachedToken != read)
                return;

            cachedToken = _provider();
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}