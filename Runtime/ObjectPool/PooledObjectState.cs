namespace UnityEngine.Extension
{
    /// <summary>
    /// Where a pooled object sits in its lifecycle. Read it from <see cref="IPooledObjectHandle.State"/>
    /// to tell a live object from one that has already gone home.
    /// </summary>
    public enum PooledObjectState
    {
        /// <summary>No pool owns the object. Either it was never pooled, or its pool has let it go.</summary>
        Detached = 0,

        /// <summary>Handed out by the pool and live in the world.</summary>
        Active = 1,

        /// <summary>Held by the pool, deactivated, waiting to be handed out again.</summary>
        Pooled = 2,

        /// <summary>Destroyed, or on its way there this frame. Terminal.</summary>
        Destroyed = 3
    }
}
