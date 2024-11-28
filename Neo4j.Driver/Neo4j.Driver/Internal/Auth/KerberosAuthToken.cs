namespace Neo4j.Driver.Internal.Auth;

internal class KerberosAuthToken : FullTokenCacheKeyAuthToken
{
    public KerberosAuthToken(string base64EncodedTicket)
    {
        Content[SchemeKey] = "kerberos";
        Content[PrincipalKey] = string.Empty; // This empty string is required for backwards compatibility.
        Content[CredentialsKey] = base64EncodedTicket;
    }
}
