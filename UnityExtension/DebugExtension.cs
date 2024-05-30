namespace UnityEngine.Extension
{
    public static class DebugExtension
    {
        public static void DrawSquare(float size, Vector2 location, Color colour)
        {
            Vector2 topLeft = new Vector2(location.x + (size / 2.0F), location.y - (size / 2.0F));
            Vector2 topRight = new Vector2(location.x + (size / 2.0F), location.y + (size / 2.0F));

            Vector2 bottomLeft = new Vector2(location.x - (size / 2.0F), location.y - (size / 2.0F));
            Vector2 bottomRight = new Vector2(location.x - (size / 2.0F), location.y + (size / 2.0F));

            Debug.DrawLine(topLeft, topRight, colour);
            Debug.DrawLine(topRight, bottomRight, colour);
            Debug.DrawLine(bottomRight, bottomLeft, colour);
            Debug.DrawLine(bottomLeft, topLeft, colour);
        }

        public static void DrawBox(Bounds box, Color colour, float duration = 0.0F, bool depthTest = true)
        {
            DrawBox(box, Quaternion.identity, colour, duration, depthTest);
        }

        public static void DrawBox(Bounds box, Quaternion rotation, Color colour, float duration = 0.0F, bool depthTest = true)
        {
            DrawBox(box.extents, box.center, rotation, colour, duration, depthTest);
        }

        public static void DrawBox(Bounds box, Transform transformRef, Color colour, float duration = 0.0F, bool depthTest = true)
        {
            DrawBox(box.extents, transformRef.position, transformRef.rotation, colour, duration, depthTest);
        }

        public static void DrawBox(Vector3 extents, Vector3 centre, Quaternion rotaion, Color colour, float duration = 0.0F, bool depthTest = true)
        {
            Vector3 up = rotaion * Vector3.up;
            Vector3 right = rotaion * Vector3.right;
            Vector3 forward = rotaion * Vector3.forward;

            Vector3[] front = new Vector3[4];
            Vector3[] back = new Vector3[4];

            front[0] = centre + ((forward * extents.z) + (-right * extents.x) + (-up * extents.y));
            front[1] = centre + ((forward * extents.z) + (-right * extents.x) + (up * extents.y));
            front[2] = centre + ((forward * extents.z) + (right * extents.x) + (up * extents.y));
            front[3] = centre + ((forward * extents.z) + (right * extents.x) + (-up * extents.y));

            back[0] = centre + ((-forward * extents.z) + (-right * extents.x) + (-up * extents.y));
            back[1] = centre + ((-forward * extents.z) + (-right * extents.x) + (up * extents.y));
            back[2] = centre + ((-forward * extents.z) + (right * extents.x) + (up * extents.y));
            back[3] = centre + ((-forward * extents.z) + (right * extents.x) + (-up * extents.y));

            DrawBox(front, back, colour, duration, depthTest);
        }

        public static void DrawBox(Vector3[] frontCorners, Vector3[] backCorners, Color colour, float duration = 0.0F, bool depthTest = true)
        {
            if (frontCorners.Length != 4 || backCorners.Length != 4)
            {
                Debug.LogError("DebugHelper - DrawCube expected 2 Vector3[] of length 4 (0 - Bottom Left, 1 - Top Left, 2 - Top Right, 3 - Bottom Right)");
                return;
            }
            //Front square
            Debug.DrawLine(frontCorners[0], frontCorners[1], colour, duration, depthTest);
            Debug.DrawLine(frontCorners[1], frontCorners[2], colour, duration, depthTest);
            Debug.DrawLine(frontCorners[2], frontCorners[3], colour, duration, depthTest);
            Debug.DrawLine(frontCorners[3], frontCorners[0], colour, duration, depthTest);

            //Back square
            Debug.DrawLine(backCorners[0], backCorners[1], colour, duration, depthTest);
            Debug.DrawLine(backCorners[1], backCorners[2], colour, duration, depthTest);
            Debug.DrawLine(backCorners[2], backCorners[3], colour, duration, depthTest);
            Debug.DrawLine(backCorners[3], backCorners[0], colour, duration, depthTest);

            //Attaching back and front        
            Debug.DrawLine(backCorners[0], frontCorners[0], colour, duration, depthTest);
            Debug.DrawLine(backCorners[1], frontCorners[1], colour, duration, depthTest);
            Debug.DrawLine(backCorners[2], frontCorners[2], colour, duration, depthTest);
            Debug.DrawLine(backCorners[3], frontCorners[3], colour, duration, depthTest);
        }
    }
}
