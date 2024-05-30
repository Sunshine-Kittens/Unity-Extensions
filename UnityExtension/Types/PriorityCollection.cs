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
            public T element { get; private set; }
            public int priority { get; private set; }

            public static implicit operator PrioritizedElement(T element)
            {
                return new PrioritizedElement(element, 0);
            }

            public override int GetHashCode()
            {
                return element.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return element.Equals(obj);
            }

            public bool Equals(T other)
            {
                return element.Equals(other);
            }

            public PrioritizedElement(T element, int priority)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                this.element = element;
                this.priority = priority;
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
            _groups.TryGetValue(prioritizedElement.priority, out group);
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
            private PriorityCollection<T> collection;
            private T current;

            int groupIndex;
            int elementIndex;

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
                    return current;
                }
            }

            internal Enumerator(PriorityCollection<T> collection)
            {
                this.collection = collection;
                groupIndex = 0;
                elementIndex = 0;
                current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (groupIndex < collection._groups.Count)
                {
                    if (elementIndex < collection._groups.Values[groupIndex].Count)
                    {
                        current = collection._groups.Values[groupIndex][elementIndex];

                        if (elementIndex < collection._groups.Values[groupIndex].Count - 1)
                        {
                            elementIndex++;
                        }
                        else
                        {
                            elementIndex = 0;
                            groupIndex++;
                        }
                        return true;
                    }
                }
                return false;
            }

            void IEnumerator.Reset()
            {
                groupIndex = 0;
                elementIndex = 0;
                current = default(T);
            }
        }
    }
}