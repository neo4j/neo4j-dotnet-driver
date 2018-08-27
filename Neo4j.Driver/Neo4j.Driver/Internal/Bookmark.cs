// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class Bookmark
    {
        internal const string BookmarkKey = "bookmark";
        internal const string BookmarksKey = "bookmarks";

        private const long UnknownBookmarkValue = -1;
        internal const string BookmarkPrefix = "neo4j:bookmark:v1:tx";

        private readonly IEnumerable<string> _values;// nullable or contain null items
        private readonly string _maxBookmark;        // nullable
        private readonly ILogger _logger;

        private Bookmark(IEnumerable<string> values, ILogger logger)
        {
            _logger = logger;
            _values = values;
            _maxBookmark = ComputeMaxBookmark(values);
        }

        public static Bookmark From(string bookmark, ILogger logger = null)
        {
            if (bookmark == null)
            {
                return new Bookmark(null, logger);
            }
            return new Bookmark(new []{bookmark}, logger);
        }

        public static Bookmark From(IEnumerable<string> values, ILogger logger = null)
        {
            return new Bookmark(values, logger);
        }

        public string MaxBookmark()
        {
            return _maxBookmark;
        }

        public IEnumerable<string> Bookmarks()
        {
            return _values;
        }

        public bool IsEmpty()
        {
            return _values == null || _maxBookmark == null;
        }

        public IDictionary<string, object> AsBeginTransactionParameters()
        {
            if (IsEmpty())
            {
                return null;
            }
            return new Dictionary<string, object>
            {
                {BookmarksKey, _values}, {BookmarkKey, _maxBookmark}
            };
        }

        private string ComputeMaxBookmark(IEnumerable<string> values)
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

        private long BookmarkValue(string value)
        {
            if (value == null)
            {
                return UnknownBookmarkValue;
            }
            if (!value.StartsWith(BookmarkPrefix))
            {
                LogIllegalBookmark(value);
                return UnknownBookmarkValue;
            }
            try
            {
                return Convert.ToInt64(value.Substring(BookmarkPrefix.Length));
            }
            catch (FormatException)
            {
                LogIllegalBookmark(value);
                return UnknownBookmarkValue;
            }
        }

        private void LogIllegalBookmark(string value)
        {
            _logger?.Info($"Failed to recognize bookmark '{value}' and this bookmark is ignored.");
        }
    }
}
