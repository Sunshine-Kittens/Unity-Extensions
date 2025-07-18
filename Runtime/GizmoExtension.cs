namespace UnityEngine.Extension
{
    public static class GizmoExtension
    {
        public static void DrawSquare(float size, Vector2 location, Color colour)
        {
            Vector2 topLeft = new Vector2(location.x + (size / 2.0F), location.y - (size / 2.0F));
            Vector2 topRight = new Vector2(location.x + (size / 2.0F), location.y + (size / 2.0F));

            Vector2 bottomLeft = new Vector2(location.x - (size / 2.0F), location.y - (size / 2.0F));
            Vector2 bottomRight = new Vector2(location.x - (size / 2.0F), location.y + (size / 2.0F));

            Gizmos.color = colour;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
    }
}
