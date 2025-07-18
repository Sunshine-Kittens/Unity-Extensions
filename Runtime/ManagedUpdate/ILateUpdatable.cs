namespace UnityEngine.Extension
{
    public interface ILateUpdatable : IManagedObject
    {
        public void ManagedLateUpdate();
    }
}