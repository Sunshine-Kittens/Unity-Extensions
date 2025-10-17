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

            public readonly List<IUpdatable> List = new List<IUpdatable>();

            public ManagedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    if (List[i].Active)
                    {
                        List[i].ManagedUpdate();
                    }                    
                }
            }
        }

        private class ManagedLateUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public readonly List<ILateUpdatable> List = new List<ILateUpdatable>();
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate);

            public ManagedLateUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    if (List[i].Active)
                    {
                        List[i].ManagedLateUpdate();
                    }
                }
            }
        }

        private class ManagedFixedUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public readonly List<IFixedUpdatable> List = new List<IFixedUpdatable>();
            public EntryPointLocation Location => EntryPointLocation.Before;
            public Type EntryPoint => typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate);

            public ManagedFixedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    if (List[i].Active)
                    {
                        List[i].ManagedFixedUpdate();
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
            _updatablesPlayerLoopSystem.List.Add(updatable);
        }

        public static void RemoveUpdatable(IUpdatable updatable)
        {
            _updatablesPlayerLoopSystem.List.Remove(updatable);
        }

        public static void AddLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.List.Add(lateUpdatable);
        }

        public static void RemoveLateUpdatable(ILateUpdatable lateUpdatable)
        {
            _lateUpdatablesPlayerLoopSystem.List.Remove(lateUpdatable);
        }

        public static void AddFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.List.Add(fixedUpdatable);
        }

        public static void RemoveFixedUpdatable(IFixedUpdatable fixedUpdatable)
        {
            _fixedUpdatablesPlayerLoopSystem.List.Remove(fixedUpdatable);
        }
    }
}