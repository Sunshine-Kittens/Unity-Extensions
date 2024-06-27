using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public class UpdateManager : Singleton<UpdateManager>
    {
        private static HashSet<IUpdatable> _updatablesHashSet = new HashSet<IUpdatable>();
        private static List<IUpdatable> _updatablesList = new List<IUpdatable>();

        private static HashSet<ILateUpdatable> _lateUpdatablesHashSet = new HashSet<ILateUpdatable>();
        private static List<ILateUpdatable> _lateUpdatablesList = new List<ILateUpdatable>();

        private static HashSet<IFixedUpdatable> _fixedUpdatablesHashSet = new HashSet<IFixedUpdatable>();
        private static List<IFixedUpdatable> _fixedUpdatablesList = new List<IFixedUpdatable>();

        protected override bool Persists { get { return true; } }

        public static void AddUpdatable(IUpdatable updatable)
        {
            if (_updatablesHashSet.Contains(updatable))
            {
                throw new InvalidOperationException("IUpdatable has already been added to the manager.");
            }

            _updatablesHashSet.Add(updatable);
            _updatablesList.Add(updatable);
        }

        public static void RemoveUpdatable(IUpdatable updatable)
        {
            if (!_updatablesHashSet.Contains(updatable))
            {
                throw new InvalidOperationException("IUpdatable has not been added to the manager.");
            }

            _updatablesHashSet.Remove(updatable);
            _updatablesList.Remove(updatable);
        }

        public static void AddLateUpdatable(ILateUpdatable lateUpdatable)
        {
            if (_lateUpdatablesHashSet.Contains(lateUpdatable))
            {
                throw new InvalidOperationException("ILateUpdatable has already been added to the manager.");
            }

            _lateUpdatablesHashSet.Add(lateUpdatable);
            _lateUpdatablesList.Add(lateUpdatable);
        }

        public static void RemoveLateUpdatable(ILateUpdatable lateUpdatable)
        {
            if (!_lateUpdatablesHashSet.Contains(lateUpdatable))
            {
                throw new InvalidOperationException("ILateUpdatable has not been added to the manager.");
            }

            _lateUpdatablesHashSet.Remove(lateUpdatable);
            _lateUpdatablesList.Remove(lateUpdatable);
        }

        public static void AddFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            if (_fixedUpdatablesHashSet.Contains(fixedUpdatable))
            {
                throw new InvalidOperationException("IFixedUpdatable has already been added to the manager.");
            }

            _fixedUpdatablesHashSet.Add(fixedUpdatable);
            _fixedUpdatablesList.Add(fixedUpdatable);
        }

        public static void RemoveFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            if (!_fixedUpdatablesHashSet.Contains(fixedUpdatable))
            {
                throw new InvalidOperationException("IFixedUpdatable has not been added to the manager.");
            }

            _fixedUpdatablesHashSet.Remove(fixedUpdatable);
            _fixedUpdatablesList.Remove(fixedUpdatable);
        }

        private void Update()
        {
            for (int i = 0; i < _updatablesList.Count; i++)
            {
                _updatablesList[i].ManagedUpdate();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdatablesList.Count; i++)
            {
                _lateUpdatablesList[i].ManagedLateUpdate();
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _fixedUpdatablesList.Count; i++)
            {
                _fixedUpdatablesList[i].ManagedFixedUpdate();
            }
        }
    }
}