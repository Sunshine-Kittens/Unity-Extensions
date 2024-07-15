using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    // Not thread safe :(
    public class PriorityCollection<T> : IEnumerable<T>
    {
        private struct Comparison : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return -x.CompareTo(y);
            }
        }

        private struct PrioritizedElement : IEquatable<T>
        {
            public T Element { get; private set; }
            public int Priority { get; private set; }

            public static implicit operator PrioritizedElement(T element)
            {
                return new PrioritizedElement(element, 0);
            }

            public override int GetHashCode()
            {
                return Element.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Element.Equals(obj);
            }

            public bool Equals(T other)
            {
                return Element.Equals(other);
            }

            public PrioritizedElement(T element, int priority)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                this.Element = element;
                this.Priority = priority;
            }
        }

        private const int _defaultGroupsCapacity = 8;
        private const int _defaultGroupCapacity = 4;

        private HashSet<PrioritizedElement> _elements = new HashSet<PrioritizedElement>(_defaultGroupCapacity * _defaultGroupsCapacity);
        private SortedList<int, List<T>> _groups = new SortedList<int, List<T>>(_defaultGroupsCapacity, new Comparison());

        public bool Add(in T element, int priority = 0)
        {
            if (!_elements.Add(new PrioritizedElement(element, priority)))
            {
                return false;
            }

            List<T> group;
            if (!_groups.TryGetValue(priority, out group))
            {
                group = new List<T>(_defaultGroupCapacity);
                _groups.Add(priority, group);
            }

            group.Add(element);
            return true;
        }

        public bool Remove(in T element)
        {
            PrioritizedElement prioritizedElement;
            if (!_elements.TryGetValue(element, out prioritizedElement))
            {
                return false;
            }
            _elements.Remove(prioritizedElement);

            List<T> group;
            _groups.TryGetValue(prioritizedElement.Priority, out group);
            group.Remove(element);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private PriorityCollection<T> _collection;
            private T _current;

            private int _groupIndex;
            private int _elementIndex;

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public T Current
            {
                get
                {
                    return _current;
                }
            }

            internal Enumerator(PriorityCollection<T> collection)
            {
                this._collection = collection;
                _groupIndex = 0;
                _elementIndex = 0;
                _current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (_groupIndex < _collection._groups.Count)
                {
                    if (_elementIndex < _collection._groups.Values[_groupIndex].Count)
                    {
                        _current = _collection._groups.Values[_groupIndex][_elementIndex];

                        if (_elementIndex < _collection._groups.Values[_groupIndex].Count - 1)
                        {
                            _elementIndex++;
                        }
                        else
                        {
                            _elementIndex = 0;
                            _groupIndex++;
                        }
                        return true;
                    }
                }
                return false;
            }

            void IEnumerator.Reset()
            {
                _groupIndex = 0;
                _elementIndex = 0;
                _current = default(T);
            }
        }
    }
}