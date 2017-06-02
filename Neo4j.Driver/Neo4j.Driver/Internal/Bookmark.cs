using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal
{
    internal class Bookmark
    {
        internal const string BookmarkKey = "bookmark";
        private const string BookmarksKey = "bookmarks";

        private const long UnknownBookmarkValue = -1;
        internal const string BookmarkPrefix = "neo4j:bookmark:v1:tx";

        private readonly IEnumerable<string> _values;
        private readonly string _maxBookmark;

        private Bookmark(IEnumerable<string> values)
        {
            _values = values;
            _maxBookmark = MaxBookmark(values);
        }

        public static Bookmark From(string bookmark)
        {
            if (bookmark == null)
            {
                return new Bookmark(null);
            }
            return new Bookmark(new []{bookmark});
        }

        public static Bookmark From(IEnumerable<string> values)
        {
            return new Bookmark(values);
        }

        public string MaxBookmarkAsString()
        {
            return _maxBookmark;
        }

        public bool IsEmpty()
        {
            return _values == null || _maxBookmark == null;
        }

        public IDictionary<string, object> AsBeginTransactionParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                {BookmarksKey, _values}, {BookmarkKey, _maxBookmark}
            };
            return parameters;
        }

        private string MaxBookmark(IEnumerable<string> values)
        {
            if (values == null)
            {
                return null;
            }
            var maxValue = UnknownBookmarkValue;
            foreach (var value in values)
            {
                var curValue = BookmarkValue(value);
                if (curValue > maxValue)
                {
                    maxValue = curValue;
                }
            }
            if (maxValue != UnknownBookmarkValue)
            {
                return $"{BookmarkPrefix}{maxValue}";
            }
            return null;
        }

        private static long BookmarkValue(string value)
        {
            if (value != null && value.StartsWith(BookmarkPrefix))
            {
                try
                {
                    return Convert.ToInt64(value.Substring(BookmarkPrefix.Length));
                }
                catch (FormatException)
                {
                    return UnknownBookmarkValue;
                }
            }
            return UnknownBookmarkValue;
        }
    }
}