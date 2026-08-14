using System;

namespace UnityEngine.Extension
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get { return _instance; } }
        private static T _instance = null;

        protected abstract bool Persists { get; }

        public static bool IsInstanceValid()
        {
            return _instance != null;
        }

        protected virtual void Awake()
        {
            if (_instance != null)
            {
                throw new Exception("More than once instance of" + nameof(T) + " exists within the current scene.");
            }

            if (Persists)
            {
                if (transform.parent != null)
                    Debug.LogWarning("Singleton is unable to persist as it is not at the scene root");
                else
                    DontDestroyOnLoad(gameObject);
            }
            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
        }
    }
}