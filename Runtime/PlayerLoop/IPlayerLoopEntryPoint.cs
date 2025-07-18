using System;

namespace UnityEngine.Extension
{
    public enum EntryPointLocation
    {
        Before,
        After,
        Replace
    }

    public interface IPlayerLoopEntryPoint
    {
        public EntryPointLocation Location { get; }
        public Type EntryPoint { get; }
    }
}
