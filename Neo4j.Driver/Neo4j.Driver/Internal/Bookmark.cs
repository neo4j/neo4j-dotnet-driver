// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;

namespace Neo4j.Driver.Internal
{
    internal class Bookmark
    {
        internal const string BookmarkPrefix = "neo4j:bookmark:v1:tx";
        internal const string BookmarkKey = "bookmark";
        internal const string BookmarksKey = "bookmarks";
        internal const long UnknownBookmarkValue = -1;

        private readonly IEnumerable<string> _values; // nullable or contain null items
        private readonly string _maxBookmark;

        private Bookmark(string[] values)
        {
            _values = values;
            _maxBookmark = values?.Select(x => (bookmark: x, value: BookmarkValue(x)))
                .Where(vt => vt.value > UnknownBookmarkValue)
                .OrderByDescending(vt => vt.value)
                .Select(vt => vt.bookmark)
                .FirstOrDefault();
        }

        public static Bookmark From(string bookmark)
        {
            return string.IsNullOrEmpty(bookmark) ? new Bookmark(null) : new Bookmark(new[] {bookmark});
        }

        public static Bookmark From(IEnumerable<string> values)
        {
            return new Bookmark(values.ToArray());
        }

        public string MaxBookmark => _maxBookmark;

        public IEnumerable<string> Bookmarks => _values;

        public bool HasBookmark => _values != null && _maxBookmark != null;

        protected bool Equals(Bookmark other)
        {
            if (_values == null && other._values == null)
            {
                return true;
            }

            if (_values == null || other._values == null)
            {
                return false;
            }

            return _values.SequenceEqual(other._values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Bookmark) obj);
        }

        public override int GetHashCode()
        {
            return (_values != null ? _values.GetHashCode() : 0);
        }

        public IDictionary<string, object> AsBeginTransactionParameters()
        {
            if (HasBookmark)
            {
                return new Dictionary<string, object>
                {
                    {BookmarksKey, _values}, {BookmarkKey, _maxBookmark}
                };
            }

            return null;
        }

        private static long BookmarkValue(string value)
        {
            if (value == null)
            {
                return UnknownBookmarkValue;
            }

            if (!value.StartsWith(BookmarkPrefix))
            {
                return UnknownBookmarkValue;
            }

            try
            {
                return Convert.ToInt64(value.Substring(BookmarkPrefix.Length));
            }
            catch (FormatException)
            {
                return UnknownBookmarkValue;
            }
        }
    }
}