// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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

namespace Neo4j.Driver.Internal.Result
{
    internal interface IMessageResponseCollector
    {
        // success
        void CollectFields(IDictionary<string, object> meta);
        void CollectBookmark(IDictionary<string, object> meta);
        void CollectSummary(IDictionary<string, object> meta);

        // record
        void CollectRecord(object[] fields);

        void DoneSuccess();
        void DoneFailure();
        void DoneIgnored();
    }

    internal class BookmarkCollector : NoOperationCollector
    {
        private readonly Action<string> _setBookmarkCallbackAction;
        public BookmarkCollector(Action<string> bookmarkCallbackAction)
        {
            _setBookmarkCallbackAction = bookmarkCallbackAction;
        }

        public override void CollectBookmark(IDictionary<string, object> meta)
        {
            if (meta.ContainsKey(Bookmark.BookmarkKey))
            {
                var bookmark = meta[Bookmark.BookmarkKey].As<string>();
                _setBookmarkCallbackAction.Invoke(bookmark);
            }
        }
    }

    internal class ResetCollector : NoOperationCollector
    {
        private readonly Action _successCallbackAction;
        public ResetCollector(Action successCallBackAction = null)
        {
            _successCallbackAction = successCallBackAction ?? (() => { });
        }

        public override void DoneSuccess()
        {
            _successCallbackAction.Invoke();
        }
    }

    internal class InitCollector : NoOperationCollector
    {
        public string Server { private set; get; }
        public override void CollectSummary(IDictionary<string, object> meta)
        {
            if (meta.ContainsKey("server"))
            {
                Server = meta["server"].As<string>();
            }
        }
    }

    internal abstract class NoOperationCollector : IMessageResponseCollector
    {
        public virtual void CollectFields(IDictionary<string, object> meta)
        {
            // left empty
        }

        public virtual void CollectBookmark(IDictionary<string, object> meta)
        {
            // left empty
        }

        public virtual void CollectRecord(object[] fields)
        {
            // left empty
        }

        public virtual void CollectSummary(IDictionary<string, object> meta)
        {
            // left empty
        }

        public virtual void DoneSuccess()
        {
            // left empty
        }

        public virtual void DoneFailure()
        {
            // left empty
        }

        public virtual void DoneIgnored()
        {
            // left empty
        }
    }
}
