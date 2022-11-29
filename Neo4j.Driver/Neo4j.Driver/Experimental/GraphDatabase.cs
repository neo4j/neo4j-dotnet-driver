using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Methods being considered for moving to the Neo4j.Driver.
/// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
/// </summary>
public static class GraphDatabase
{
    /// <summary>
    /// Experimental: Bookmark Manager API is still under consideration. <br/>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.<br/>
    /// Gets a new <see cref="IBookmarkManagerFactory"/>, which can construct
    /// a new default <see cref="IBookmarkManager"/> instance.<br/>
    /// The <see cref="IBookmarkManager"/> instance should be passed to <see cref="SessionConfigBuilder"/> when opening
    /// a new session with <see cref="ExperimentalExtensions.WithBookmarkManager"/>.
    /// </summary>
    public static IBookmarkManagerFactory BookmarkManagerFactory => new BookmarkManagerFactory();
}