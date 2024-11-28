namespace Neo4j.Driver.Internal.Auth;

internal class BasicAuthToken : AuthToken
{
    public BasicAuthToken(string username, string password, string realm = null)
    {
        Content[SchemeKey] = "basic";
        Content[PrincipalKey] = username;
        Content[CredentialsKey] = password;
        if (realm != null)
        {
            Content[RealmKey] = realm;
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Content[PrincipalKey].GetHashCode();
    }
}
