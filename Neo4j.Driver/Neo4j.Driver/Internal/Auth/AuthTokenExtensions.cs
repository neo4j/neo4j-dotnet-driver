using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Auth;

internal static class AuthTokenExtensions
{
    public static IDictionary<string, object> AsDictionary(this IAuthToken authToken)
    {
        if (authToken is not AuthToken token)
        {
            throw new ClientException(
                $"Unknown authentication token, `{authToken}`. Please use one of the supported " +
                $"tokens from `{nameof(AuthTokens)}`.");
        }

        return token.Content
            .Where(kvp => kvp.Value is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}