using System;

namespace UnityEngine.Extension
{
    public enum EntryPointLocation
    {
        Before,
        After,
        InPlace
    }

    public interface IPlayerLoopEntryPoint
    {
        public EntryPointLocation Location { get; }
        public Type EntryPoint { get; }
    }
}
