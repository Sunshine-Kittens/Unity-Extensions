using System;
using System.Collections.Generic;

using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public static class UpdateManager
    {
        private class ManagedUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public EntryPointLocation Location { get { return EntryPointLocation.Before; } }
            public Type EntryPoint { get { return typeof(Update.ScriptRunBehaviourUpdate); } }

            public List<IUpdatable> List = new List<IUpdatable>();

            public ManagedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    List[i].ManagedUpdate();
                }
            }
        }

        private class ManagedLateUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public List<ILateUpdatable> List = new List<ILateUpdatable>();
            public EntryPointLocation Location { get { return EntryPointLocation.Before; } }
            public Type EntryPoint { get { return typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate); } }

            public ManagedLateUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    List[i].ManagedLateUpdate();
                }
            }
        }

        private class ManagedFixedUpdatePlayerLoopSystem : IPlayerLoopSystem
        {
            public List<IFixedUpdatable> List = new List<IFixedUpdatable>();
            public EntryPointLocation Location { get { return EntryPointLocation.Before; } }
            public Type EntryPoint { get { return typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate); } }

            public ManagedFixedUpdatePlayerLoopSystem() { }

            public void Update()
            {
                for (int i = 0; i < List.Count; i++)
                {
                    List[i].ManagedFixedUpdate();
                }
            }
        }

        public static IPlayerLoopSystem UpdatablesPlayerLoopSystem { get { return _updatablesPlayerLoopSystem; } }
        private static ManagedUpdatePlayerLoopSystem _updatablesPlayerLoopSystem = new ManagedUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem LateUpdatablesPlayerLoopSystem { get { return _lateUpdatablesPlayerLoopSystem; } }
        private static ManagedLateUpdatePlayerLoopSystem _lateUpdatablesPlayerLoopSystem = new ManagedLateUpdatePlayerLoopSystem();
        public static IPlayerLoopSystem FixedUpdatablesPlayerLoopSystem { get { return _fixedUpdatablesPlayerLoopSystem; } }
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