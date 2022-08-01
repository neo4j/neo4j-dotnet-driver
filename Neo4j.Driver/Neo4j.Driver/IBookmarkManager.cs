using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// 
/// </summary>
public interface IBookmarkManager
{
    /// <summary>
    /// Updates the bookmark manager's last known bookmarks.
    /// </summary>
    /// <param name="database">Database which the bookmarks belong to.</param>
    /// <param name="previousBookmarks">The bookmarks used at the start of bookmark.</param>
    /// <param name="newBookmarks">The bookmarks to replace previousBookmarks with.</param>
    void UpdateBookmarks(string database, string[] previousBookmarks, string[] newBookmarks);

    /// <summary>
    /// Retrieves last known bookmarks for a database.
    /// </summary>
    /// <param name="database">Database to get latest known bookmarks for.</param>
    /// <returns>Last known bookmarks for database.</returns>
    Bookmarks GetBookmarks(string database);

    /// <summary>
    /// Retrieves all bookmarks.
    /// </summary>
    /// <param name="databases">Databases the result must include.</param>
    /// <returns>Last known bookmarks for all databases.</returns>
    string[] GetAllBookmarks(params string[] databases);
}