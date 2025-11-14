using System;
using System.Collections.Generic;

using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public static class UpdateManager
    {
        private abstract class ManagedUpdateLoopSystemBase<T> : IPlayerLoopSystem where T : IManagedObject
        {
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(Update.ScriptRunBehaviourUpdate);

            private readonly List<T> _items = new List<T>();
            private readonly Dictionary<T, int> _indexMap = new Dictionary<T, int>();

            private readonly List<T> _deferredRemovals = new List<T>();
            private readonly HashSet<T> _deferredRemovalSet = new HashSet<T>();

            private bool _isIterating = false;

            public ManagedUpdateLoopSystemBase() { }
            
            public bool Contains(T item) => item != null && _indexMap.ContainsKey(item);
            public int Count => _items.Count;
            public T[] ToArray() => _items.ToArray();
            
            protected abstract void CallUpdate(T item);
            
            public void Update()
            {
                _isIterating = true;
                try
                {
                    for (int i = 0; i < _items.Count; ++i)
                    {
                        T item = _items[i];
                        CallUpdate(item);
                    }
                }
                finally
                {
                    _isIterating = false;
                }
                
                if (_deferredRemovals.Count > 0)
                {
                    for (int i = 0; i < _deferredRemovals.Count; ++i)
                    {
                        Remove(_deferredRemovals[i]);
                    }
                    _deferredRemovals.Clear();
                    _deferredRemovalSet.Clear();
                }
            }
            
            public void Add(T item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                if (_indexMap.ContainsKey(item)) return;
                
                if (_deferredRemovalSet.Contains(item))
                {
                    _deferredRemovalSet.Remove(item);
                    int last = _deferredRemovals.Count - 1;
                    for (int i = 0; i <= last; ++i)
                    {
                        if (EqualityComparer<T>.Default.Equals(_deferredRemovals[i], item))
                        {
                            if (i != last)
                                _deferredRemovals[i] = _deferredRemovals[last];
                            _deferredRemovals.RemoveAt(last);
                            break;
                        }
                    }
                }
                int index = _items.Count;
                _items.Add(item);
                _indexMap[item] = index;
            }
            
            public bool Remove(T item)
            {
                if (_isIterating)
                    return DeferRemoval(item);
                return RemoveImmediate(item);
            }
            
            private bool RemoveImmediate(T item)
            {
                if (item == null) return false;
                if (!_indexMap.TryGetValue(item, out int index)) return false;

                int last = _items.Count - 1;
                T lastItem = _items[last];
                
                if (index != last)
                {
                    _items[index] = lastItem;
                    _indexMap[lastItem] = index;
                }
                
                _items.RemoveAt(last);
                _indexMap.Remove(item);
                return true;
            }
            
            private bool DeferRemoval(T item)
            {
                if (item == null || !_deferredRemovalSet.Add(item)) 
                    return false;
                _deferredRemovals.Add(item);
                return true;
            }
        }

        private class ManagedUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<IUpdatable>
        {
            protected override void CallUpdate(IUpdatable item)
            {
                item.ManagedUpdate();
            }
        }

        private class ManagedLateUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<ILateUpdatable>
        {
            protected override void CallUpdate(ILateUpdatable item)
            {
                item.ManagedLateUpdate();
            }
        }

        private class ManagedFixedUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<IFixedUpdatable>
        {
            protected override void CallUpdate(IFixedUpdatable item)
            {
                item.ManagedFixedUpdate();
            }
        }

        public static IPlayerLoopSystem UpdatablesPlayerLoopSystem => _updatablesPlayerLoopSystem;
        private static ManagedUpdatePlayerLoopSystem _updatablesPlayerLoopSystem = new ManagedUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem LateUpdatablesPlayerLoopSystem => _lateUpdatablesPlayerLoopSystem;
        private static ManagedLateUpdatePlayerLoopSystem _lateUpdatablesPlayerLoopSystem = new ManagedLateUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem FixedUpdatablesPlayerLoopSystem => _fixedUpdatablesPlayerLoopSystem;
        private static ManagedFixedUpdatePlayerLoopSystem _fixedUpdatablesPlayerLoopSystem = new ManagedFixedUpdatePlayerLoopSystem();

        static UpdateManager() { }

        public static void AddUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.Add(updatable);
        }

        public static void RemoveUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.Remove(updatable);
        }

        public static void AddLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Add(lateUpdatable);
        }

        public static void RemoveLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Remove(lateUpdatable);
        }

        public static void AddFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Add(fixedUpdatable);
        }

        public static void RemoveFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Remove(fixedUpdatable);
        }
    }
}