namespace UnityEngine.Extension
{
    public interface IManagedObject
    {
        public bool activeSelf { get; }
        public bool activeInHierarchy { get; }
    }
}
