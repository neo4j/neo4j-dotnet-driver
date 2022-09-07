using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Methods being considered for moving to the Neo4j.Driver.GraphDatabase.
/// </summary>
public static class GraphDatabase
{
    /// <summary>
    /// Experimental: Bookmark Manager API is still under consideration. <br/>
    /// Gets a new <see cref="IBookmarkManagerFactory"/>, which can construct a default implementation of <see cref="IBookmarkManager"/>.<br/>
    /// <see cref="IBookmarkManager"/> instances can be passed to <see cref="SessionConfigBuilder"/> when opening a new session.
    /// </summary>
    public static IBookmarkManagerFactory BookmarkManagerFactory => new BookmarkManagerFactory();
}