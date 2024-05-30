namespace UnityEngine.Extension
{
    public static class MathExtension
    {
        public static float Remap(this float value, float sourceRangeMin, float sourceRangeMax, float targetRangeMin, float targetRangeMax)
        {
            return (value - sourceRangeMin) / (sourceRangeMax - sourceRangeMin) * (targetRangeMax - targetRangeMin) + targetRangeMin;
        }
    }
}
