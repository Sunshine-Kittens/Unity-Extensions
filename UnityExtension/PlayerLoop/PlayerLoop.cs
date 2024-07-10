using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Extension
{
    public static class PlayerLoop
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            Application.quitting += RestoreDefaultPlayerLoop;
            PlayerLoopOverride.Initialize();
        }

        private static void RestoreDefaultPlayerLoop()
        {
            LowLevel.PlayerLoop.SetPlayerLoop(LowLevel.PlayerLoop.GetDefaultPlayerLoop());
            Application.quitting -= RestoreDefaultPlayerLoop;
        }

        public static void InsertSystem(IPlayerLoopSystem playerLoopSystem)
        {
            InsertSystem(playerLoopSystem.EntryPoint, playerLoopSystem.Location, playerLoopSystem);
        }

        public static void InsertSystemAfter<T>(IPlayerLoopUpdate playerLoopUpdate)
        {
            InsertSystemAfter(typeof(T), playerLoopUpdate);
        }

        public static void InsertSystemAfter(Type systemType, IPlayerLoopUpdate playerLoopUpdate)
        {
            InsertSystem(systemType, EntryPointLocation.After, playerLoopUpdate);
        }

        public static void InsertSystemBefore<T>(IPlayerLoopUpdate playerLoopUpdate)
        {
            InsertSystemBefore(typeof(T), playerLoopUpdate);
        }

        public static void InsertSystemBefore(Type systemType, IPlayerLoopUpdate playerLoopUpdate)
        {
            InsertSystem(systemType, EntryPointLocation.Before, playerLoopUpdate);
        }

        public static void ReplaceSystem<T>(IPlayerLoopUpdate playerLoopUpdate)
        {
            ReplaceSystem(typeof(T), playerLoopUpdate);
        }

        public static void ReplaceSystem(Type systemType, IPlayerLoopUpdate playerLoopUpdate)
        {
            InsertSystem(systemType, EntryPointLocation.InPlace, playerLoopUpdate);
        }

        private static LowLevel.PlayerLoopSystem ConstructPlayerLoopSystem(IPlayerLoopUpdate playerLoopUpdate)
        {
            return new LowLevel.PlayerLoopSystem { type = playerLoopUpdate.GetType(), updateDelegate = playerLoopUpdate.Update };
        }

        private static void InsertSystem(Type systemType, EntryPointLocation location, IPlayerLoopUpdate playerLoopUpdate)
        {
            if (playerLoopUpdate == null)
            {
                throw new ArgumentNullException(nameof(playerLoopUpdate));
            }

            if (systemType == null)
            {
                throw new ArgumentNullException(nameof(systemType));
            }

            LowLevel.PlayerLoopSystem newPlayerLoopSystem = ConstructPlayerLoopSystem(playerLoopUpdate);
            LowLevel.PlayerLoopSystem currentPlayerLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            if (!InsertSystem(ref currentPlayerLoop, systemType, location, newPlayerLoopSystem))
            {
                throw new ArgumentException($"When trying to insert the type {newPlayerLoopSystem.type.Name} into the player loop, " +
                    $"{systemType.Name} could not be found in the current player loop.");
            }
            LowLevel.PlayerLoop.SetPlayerLoop(currentPlayerLoop);
        }

        private static bool InsertSystem(ref LowLevel.PlayerLoopSystem currentPlayerLoopSystem, Type systemType, EntryPointLocation location, LowLevel.PlayerLoopSystem playerSystemLoop)
        {
            LowLevel.PlayerLoopSystem[] subSystems = currentPlayerLoopSystem.subSystemList;
            if (subSystems == null)
            {
                return false;
            }

            for (int i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type == systemType)
                {
                    if (location == EntryPointLocation.InPlace)
                    {
                        subSystems[i] = playerSystemLoop;
                    }
                    else
                    {
                        int index = location == EntryPointLocation.Before ? i : i + 1;
                        Array.Resize(ref subSystems, subSystems.Length + 1);
                        Array.Copy(subSystems, index, subSystems, index + 1, subSystems.Length - index - 1);
                        subSystems[index] = playerSystemLoop;
                    }
                    return true;
                }
                else
                {
                    LowLevel.PlayerLoopSystem subSystem = subSystems[i];
                    if (InsertSystem(ref subSystem, systemType, location, playerSystemLoop))
                    {
                        subSystems[i] = subSystem;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryRemoveSystem(IPlayerLoopUpdate playerLoopUpdate)
        {
            if (playerLoopUpdate == null)
            {
                throw new ArgumentNullException(nameof(playerLoopUpdate));
            }

            LowLevel.PlayerLoopSystem currentPlayerLoop = LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            bool removed = TryRemoveSystem(ref currentPlayerLoop, playerLoopUpdate.GetType());
            LowLevel.PlayerLoop.SetPlayerLoop(currentPlayerLoop);
            return removed;
        }

        private static bool TryRemoveSystem(ref LowLevel.PlayerLoopSystem currentSystem, Type systemType)
        {
            LowLevel.PlayerLoopSystem[] subSystems = currentSystem.subSystemList;
            if (subSystems == null)
            {
                return false;
            }

            for (int i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type == systemType)
                {
                    Array.Copy(subSystems, i + 1, subSystems, i, subSystems.Length - i - 1);
                    Array.Resize(ref subSystems, subSystems.Length - 1);
                    return true;
                }

                if (TryRemoveSystem(ref subSystems[i], systemType))
                {
                    return true;
                }
            }
            return false;
        }

        public static string CurrentLoopToString()
        {
            return PrintSystemToString(LowLevel.PlayerLoop.GetCurrentPlayerLoop());
        }

        private static string PrintSystemToString(LowLevel.PlayerLoopSystem s)
        {
            List<(LowLevel.PlayerLoopSystem, int)> systems = new List<(LowLevel.PlayerLoopSystem, int)>();

            AddRecursively(s, 0);
            void AddRecursively(LowLevel.PlayerLoopSystem system, int depth)
            {
                systems.Add((system, depth));
                if (system.subSystemList != null)
                    foreach (var subsystem in system.subSystemList)
                        AddRecursively(subsystem, depth + 1);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Systems");
            sb.AppendLine("=======");
            foreach (var (system, depth) in systems)
            {
                // root system has a null type, all others has a marker type.
                Append($"System Type: {system.type?.Name ?? "NULL"}");

                // This is a C# delegate, so it's only set for functions created on the C# side.
                Append($"Delegate: {system.updateDelegate}");

                // This is a pointer, probably to the function getting run internally. Has long values (like 140700263204024) for the builtin ones concrete ones,
                // while the builtin grouping functions has 0. So UnityEngine.PlayerLoop.Update has 0, while UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate
                // has a concrete value.
                Append($"Update Function: {system.updateFunction}");

                // The loopConditionFunction seems to be a red herring. It's set to a value for only UnityEngine.PlayerLoop.FixedUpdate, but setting a different
                // system to have the same loop condition function doesn't seem to do anything
                Append($"Loop Condition Function: {system.loopConditionFunction}");

                // null rather than an empty array when it's empty.
                Append($"{system.subSystemList?.Length ?? 0} subsystems");

                void Append(string s)
                {
                    for (int i = 0; i < depth; i++)
                        sb.Append("  ");
                    sb.AppendLine(s);
                }
            }
            return sb.ToString();
        }
    }
}