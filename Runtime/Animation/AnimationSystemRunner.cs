using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public class AnimationSystemRunner : ILateUpdatable
    {
        private static AnimationSystemRunner _instance = null;
        
        public bool Active => true;
        
        private static AnimationSystemRunner GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AnimationSystemRunner();
            }
            return _instance;
        }        

        public static void AddPlayer(AnimationPlayer player)
        {
            AnimationSystemRunner instance = GetInstance();
            if (!instance._playerHashSet.Contains(player))
            {
                instance._playerHashSet.Add(player);
                instance._playerList.Add(player);
            }
        }

        public static void RemovePlayer(AnimationPlayer player)
        {
            AnimationSystemRunner instance = GetInstance();
            if (instance._playerHashSet.Remove(player))
            {
                instance._playerList.Remove(player);
            }
        }

        private HashSet<AnimationPlayer> _playerHashSet = new HashSet<AnimationPlayer>();
        private List<AnimationPlayer> _playerList = new List<AnimationPlayer>();

        public AnimationSystemRunner()
        {
            UpdateManager.AddLateUpdatable(this);
        }

        ~AnimationSystemRunner()
        {
            _instance = null;
            UpdateManager.RemoveLateUpdatable(this);
        }        

        public void ManagedLateUpdate()
        {
            for (int i = _playerList.Count - 1; i >= 0; i--)
            {
                // Update can complete the player, and a completion continuation may release it — which
                // deregisters it here — so the list can shrink underneath this loop.
                if (i >= _playerList.Count)
                    continue;

                AnimationPlayer player = _playerList[i];
                player.Update();

                // Remove by identity, and only if still registered: the index may no longer be valid.
                if (!player.IsPlaying && _playerHashSet.Remove(player))
                {
                    _playerList.Remove(player);
                }
            }
        }
    }
}