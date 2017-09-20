using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class StatementResultCursor: IStatementResultCursor
    {
        private readonly List<string> _keys;
        private readonly Func<Task<IRecord>> _nextRecordFunc;
        private readonly Func<Task<IResultSummary>> _summaryFunc;

        private bool _atEnd = false;
        private IRecord _peeked = null;
        private IRecord _current = null;

        private Task<IResultSummary> _summary;

        public StatementResultCursor(List<string> keys, Func<Task<IRecord>> nextRecordFunc, Func<Task<IResultSummary>> summaryFunc = null)
        {
            Throw.ArgumentNullException.IfNull(keys, nameof(keys));
            Throw.ArgumentNullException.IfNull(nextRecordFunc, nameof(nextRecordFunc));

            _keys = keys;
            _nextRecordFunc = nextRecordFunc;
            _summaryFunc = summaryFunc;
        }

        public IReadOnlyList<string> Keys => _keys;

        public Task<IResultSummary> SummaryAsync()
        {
            if (_summary == null)
            {
                if (_summaryFunc != null)
                {
                    _summary = _summaryFunc();
                }
                else
                {
                    _summary = Task.FromResult((IResultSummary)null);
                }
            }

            return _summary;
        }

        public async Task<IRecord> PeekAsync()
        {
            if (_peeked != null)
            {
                return _peeked;
            }

            if (_atEnd)
            {
                return null;
            }

            _peeked = await _nextRecordFunc().ConfigureAwait(false);
            if (_peeked == null)
            {
                _atEnd = true;

                return null;
            }

            return _peeked;
        }

        public async Task<IResultSummary> ConsumeAsync()
        {
            IRecord nextRecord = await _nextRecordFunc().ConfigureAwait(false);
            while (nextRecord != null)
            {
                nextRecord = await _nextRecordFunc().ConfigureAwait(false);
            }

            return await SummaryAsync().ConfigureAwait(false);
        }

        public async Task<bool> FetchAsync()
        {
            if (_peeked != null)
            {
                _current = _peeked;
                _peeked = null;
            }
            else
            {
                try
                {
                    _current = await _nextRecordFunc().ConfigureAwait(false);
                }
                finally
                {
                    if (_current == null)
                    {
                        _atEnd = true;
                    }
                }
            }

            return _current != null;
        }

        public IRecord Current
        {
            get
            {
                if (!_atEnd && (_current == null && _peeked == null))
                {
                    throw new InvalidOperationException("Tried to access Current without calling FetchAsync.");
                }

                return _current;
            }
        }
    }
}
