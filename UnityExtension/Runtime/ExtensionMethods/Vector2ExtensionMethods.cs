namespace UnityEngine.Extension
{
    public static class Vector2ExtensionMethods
    {
        public static Vector2 RotateAroundPoint(Vector2 point, Vector2 pivot, Quaternion rotation)
        {
            return (Vector2)(rotation * (point - pivot)) + pivot;
        }

        static public Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        static public float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - b.x * a.y;
        }

        static public float CrossDot(Vector2 a, Vector2 b)
        {
            float c = Cross(a, b);
            float d = Vector2.Dot(a, b);
            float absD = (1.0F - d) * 0.5F;
            return c > 0.0F ? absD : absD * -1.0F;
        }
    }
}
