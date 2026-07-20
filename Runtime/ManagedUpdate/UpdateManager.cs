using System;
using System.Collections.Generic;

using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public static class UpdateManager
    {
        public delegate void UpdateMethod();
        
        private abstract class ManagedUpdateLoopSystemBase<T> : IPlayerLoopSystem where T : IManagedObject
        {
            public EntryPointLocation Location => EntryPointLocation.Before;
            public abstract Type EntryPoint { get; }

            private readonly List<T> _items = new List<T>();
            private readonly Dictionary<T, int> _indexMap = new Dictionary<T, int>();

            private readonly List<T> _deferredRemovals = new List<T>();
            private readonly HashSet<T> _deferredRemovalSet = new HashSet<T>();
            
            private readonly List<UpdateMethod> _delegateItems = new List<UpdateMethod>();
            private readonly Dictionary<UpdateMethod, int> _delegateIndexMap = new Dictionary<UpdateMethod, int>();

            private readonly List<UpdateMethod> _deferredDelegateRemovals = new List<UpdateMethod>();
            private readonly HashSet<UpdateMethod> _deferredDelegateRemovalSet = new HashSet<UpdateMethod>();

            private bool _isIterating = false;

            protected ManagedUpdateLoopSystemBase() { }

            public bool Contains(T item) => item != null && _indexMap.ContainsKey(item);
            public bool Contains(UpdateMethod item) => item != null && _delegateIndexMap.ContainsKey(item);
            public int Count => _items.Count + _delegateItems.Count;
            public T[] ToArray() => _items.ToArray();

            protected abstract void CallUpdate(T item);

            public void Update()
            {
                _isIterating = true;
                
                for (int i = 0; i < _items.Count; ++i)
                {
                    try
                    {
                        T item = _items[i];
                        if (item.Active)
                        {
                            CallUpdate(item);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                for (int i = 0; i < _delegateItems.Count; ++i)
                {
                    try
                    {
                        _delegateItems[i].Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                
                _isIterating = false;
                    
                if (_deferredRemovals.Count > 0)
                {
                    for (int i = 0; i < _deferredRemovals.Count; ++i)
                    {
                        Remove(_deferredRemovals[i]);
                    }
                    _deferredRemovals.Clear();
                    _deferredRemovalSet.Clear();
                }

                if (_deferredDelegateRemovals.Count > 0)
                {
                    for (int i = 0; i < _deferredDelegateRemovals.Count; ++i)
                    {
                        RemoveStatic(_deferredDelegateRemovals[i]);
                    }
                    _deferredDelegateRemovals.Clear();
                    _deferredDelegateRemovalSet.Clear();
                }
            }

            public void Add(T item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                
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
                    return;
                }

                if (_indexMap.ContainsKey(item)) return;

                int index = _items.Count;
                _items.Add(item);
                _indexMap[item] = index;
            }

            public void AddStatic(UpdateMethod item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));

                if(!item.Method.IsStatic)
                    throw new InvalidOperationException($"Method {item.Method.Name} is not static");
                
                if (_deferredDelegateRemovalSet.Contains(item))
                {
                    _deferredDelegateRemovalSet.Remove(item);
                    int last = _deferredDelegateRemovals.Count - 1;
                    for (int i = 0; i <= last; ++i)
                    {
                        if (_deferredDelegateRemovals[i] == item)
                        {
                            if (i != last)
                                _deferredDelegateRemovals[i] = _deferredDelegateRemovals[last];
                            _deferredDelegateRemovals.RemoveAt(last);
                            break;
                        }
                    }
                    return;
                }

                if (_delegateIndexMap.ContainsKey(item)) return;

                int index = _delegateItems.Count;
                _delegateItems.Add(item);
                _delegateIndexMap[item] = index;
            }

            public bool Remove(T item)
            {
                if (_isIterating)
                    return DeferRemoval(item);
                return RemoveImmediate(item);
            }

            public bool RemoveStatic(UpdateMethod item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                    
                if(!item.Method.IsStatic)
                    throw new InvalidOperationException($"Method {item.Method.Name} is not static");
                
                if (_isIterating)
                    return DeferDelegateRemoval(item);
                return RemoveDelegateImmediate(item);
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

            private bool RemoveDelegateImmediate(UpdateMethod item)
            {
                if (item == null) return false;
                if (!_delegateIndexMap.TryGetValue(item, out int index)) return false;

                int last = _delegateItems.Count - 1;
                UpdateMethod lastItem = _delegateItems[last];

                if (index != last)
                {
                    _delegateItems[index] = lastItem;
                    _delegateIndexMap[lastItem] = index;
                }

                _delegateItems.RemoveAt(last);
                _delegateIndexMap.Remove(item);
                return true;
            }

            private bool DeferDelegateRemoval(UpdateMethod item)
            {
                if (item == null || !_deferredDelegateRemovalSet.Add(item))
                    return false;
                _deferredDelegateRemovals.Add(item);
                return true;
            }
        }

        private class ManagedUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<IUpdatable>
        {
            public override Type EntryPoint => typeof(Update.ScriptRunBehaviourUpdate);

            protected override void CallUpdate(IUpdatable item)
            {
                item.ManagedUpdate();
            }
        }

        private class ManagedLateUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<ILateUpdatable>
        {
            public override Type EntryPoint => typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate);

            protected override void CallUpdate(ILateUpdatable item)
            {
                item.ManagedLateUpdate();
            }
        }

        private class ManagedFixedUpdatePlayerLoopSystem : ManagedUpdateLoopSystemBase<IFixedUpdatable>
        {
            public override Type EntryPoint => typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate);

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

        public static void AddStaticUpdatable(UpdateMethod updateAction)
        {
            _updatablesPlayerLoopSystem.AddStatic(updateAction);
        }
        
        public static void RemoveUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.Remove(updatable);
        }

        public static void RemoveStaticUpdatable(UpdateMethod updateAction)
        {
            _updatablesPlayerLoopSystem.RemoveStatic(updateAction);
        }
        
        public static void AddLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Add(lateUpdatable);
        }

        public static void AddStaticLateUpdatable(UpdateMethod lateUpdateAction)
        {
            _lateUpdatablesPlayerLoopSystem.AddStatic(lateUpdateAction);
        }
        
        public static void RemoveLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Remove(lateUpdatable);
        }

        public static void RemoveStaticLateUpdatable(UpdateMethod lateUpdateAction)
        {
            _lateUpdatablesPlayerLoopSystem.RemoveStatic(lateUpdateAction);
        }
        
        public static void AddFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Add(fixedUpdatable);
        }

        public static void AddStaticFixedUpdatable(UpdateMethod fixedUpdateAction)
        {
            _fixedUpdatablesPlayerLoopSystem.AddStatic(fixedUpdateAction);
        }
        
        public static void RemoveFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Remove(fixedUpdatable);
        }

        public static void RemoveStaticFixedUpdatable(UpdateMethod fixedUpdateAction)
        {
            _fixedUpdatablesPlayerLoopSystem.RemoveStatic(fixedUpdateAction);
        }
    }
}