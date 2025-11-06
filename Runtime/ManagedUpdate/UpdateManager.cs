using System;
using System.Collections.Generic;

using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public static class UpdateManager
    {
        private class ManagedUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(Update.ScriptRunBehaviourUpdate);

            public readonly HashSet<IUpdatable> Set = new HashSet<IUpdatable>();

            public ManagedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                foreach (IUpdatable updatable in Set)
                {
                    if (updatable.Active)
                    {
                        updatable.ManagedUpdate();
                    }                    
                }
            }
        }

        private class ManagedLateUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public readonly HashSet<ILateUpdatable> Set = new HashSet<ILateUpdatable>();
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate);

            public ManagedLateUpdatePlayerLoopSystem() { }

            public void Update()
            {
                foreach (ILateUpdatable updatable in Set)
                {
                    if (updatable.Active)
                    {
                        updatable.ManagedLateUpdate();
                    }
                }
            }
        }

        private class ManagedFixedUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public readonly HashSet<IFixedUpdatable> Set = new HashSet<IFixedUpdatable>();
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate);

            public ManagedFixedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                foreach (IFixedUpdatable updatable in Set)
                {
                    if (updatable.Active)
                    {
                        updatable.ManagedFixedUpdate();
                    }
                }
            }
        }

        public static IPlayerLoopSystem UpdatablesPlayerLoopSystem => _updatablesPlayerLoopSystem;
        private static ManagedUpdatePlayerLoopSystem _updatablesPlayerLoopSystem = new ManagedUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem LateUpdatablesPlayerLoopSystem => _lateUpdatablesPlayerLoopSystem;
        private static ManagedLateUpdatePlayerLoopSystem _lateUpdatablesPlayerLoopSystem = new ManagedLateUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem FixedUpdatablesPlayerLoopSystem => _fixedUpdatablesPlayerLoopSystem;
        private static ManagedFixedUpdatePlayerLoopSystem _fixedUpdatablesPlayerLoopSystem = new ManagedFixedUpdatePlayerLoopSystem();
        
        static UpdateManager()
        {

        }

        public static void AddUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.Set.Add(updatable);
        }

        public static void RemoveUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.Set.Remove(updatable);
        }

        public static void AddLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Set.Add(lateUpdatable);
        }

        public static void RemoveLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.Set.Remove(lateUpdatable);
        }

        public static void AddFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Set.Add(fixedUpdatable);
        }

        public static void RemoveFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.Set.Remove(fixedUpdatable);
        }
    }
}