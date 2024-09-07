using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public class AnimationSystemRunner : ILateUpdatable
    {
        private static AnimationSystemRunner _instance = null;

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
                AnimationPlayer player = _playerList[i];
                player.Update();
                if (!player.IsPlaying)
                {
                    _playerList.RemoveAt(i);
                    _playerHashSet.Remove(player);
                }
            }
        }
    }
}