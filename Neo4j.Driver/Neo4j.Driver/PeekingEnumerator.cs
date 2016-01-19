using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IPeekingEnumerator<T> where T : class
    {
        bool HasNext();
        T Next();
        T Peek();
        void Discard();
    }

    public class PeekingEnumerator<T> : IPeekingEnumerator<T> where T:class
    {
        private IEnumerator<T> _enumerator;

        public PeekingEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        private T _cached;

        public bool HasNext()
        {
            return CacheNext();
        }

        public T Next()
        {
            if (CacheNext())
            {
                T result = _cached;
                _cached = null;
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the next item if it is not available yet and save it to cached.
        /// </summary>
        /// <returns></returns>
        private bool CacheNext()
        {
            if (_cached == null)
            {
                if (_enumerator == null || !_enumerator.MoveNext())
                {
                    return false;
                }

                _cached = _enumerator.Current;
                return true;
            }
            return true;
        }

        public T Peek()
        {
            return CacheNext() ? _cached : null;
        }

        public void Discard()
        {
            _cached = null;
            _enumerator = null;
        }
    }
}