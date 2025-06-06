namespace UnityEngine.Extension
{
    public interface IManagedObject : IManagedObject
    {
        public bool activeSelf { get; }
        public bool activeInHierarchy { get; }
    }
}
