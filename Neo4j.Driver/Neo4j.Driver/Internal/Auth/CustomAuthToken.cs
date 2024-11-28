using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Auth;

internal class CustomAuthToken : FullTokenCacheKeyAuthToken
{
    public CustomAuthToken(
        string principal,
        string credentials,
        string realm,
        string scheme,
        Dictionary<string, object> parameters = null)
    {
        if (principal != null)
        {
            Content[PrincipalKey] = principal;
        }

        if (!string.IsNullOrEmpty(scheme))
        {
            Content[SchemeKey] = scheme;
        }

        if (!string.IsNullOrEmpty(credentials))
        {
            Content[CredentialsKey] = credentials;
        }

        if (!string.IsNullOrEmpty(realm))
        {
            Content[RealmKey] = realm;
        }

        if (parameters != null)
        {
            Content[ParametersKey] = parameters;
        }
    }
}
