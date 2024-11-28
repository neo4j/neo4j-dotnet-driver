using System;
using System.Linq;

namespace Neo4j.Driver.Internal.Auth;

internal class BearerAuthToken : FullTokenCacheKeyAuthToken
{
    public BearerAuthToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Bearer token cannot be null or an empty string");
        }

        Content[SchemeKey] = "bearer";
        Content[CredentialsKey] = token;
    }
}
