using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public static class PlayerLoopOverride
    {
        public static void Initialize()
        {
            PlayerLoop.InsertSystem(TimerManager.PlayerLoopSystem);
            PlayerLoop.InsertSystem(UpdateManager.UpdatablesPlayerLoopSystem);
            PlayerLoop.InsertSystem(UpdateManager.LateUpdatablesPlayerLoopSystem);
            PlayerLoop.InsertSystem(UpdateManager.FixedUpdatablesPlayerLoopSystem);
        }
    }
}