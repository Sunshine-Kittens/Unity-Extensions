namespace UnityEngine.Extension
{
    public static class StaticAssert
    {
        public static void Check(bool condition, string message = null)
        {
            _ = new int[condition ? 1 : -1];
        }
    }
}
