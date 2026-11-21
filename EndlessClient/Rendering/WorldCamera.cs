using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering
{
    /// <summary>
    /// World zoom used when drawing the map/characters.
    /// Values below 1 zoom out (show more of the map).
    /// </summary>
    public static class WorldCamera
    {
        // Just a little zoomed out from the classic 1:1 view.
        public const float Zoom = 0.90f;

        public static Matrix GetTransform(int viewportWidth, int viewportHeight)
        {
            var center = new Vector3(viewportWidth / 2f, viewportHeight / 2f, 0f);
            return Matrix.CreateTranslation(-center)
                 * Matrix.CreateScale(Zoom)
                 * Matrix.CreateTranslation(center);
        }

        public static Vector2 ScreenToWorld(Vector2 screenPosition, int viewportWidth, int viewportHeight)
        {
            var center = new Vector2(viewportWidth / 2f, viewportHeight / 2f);
            return center + (screenPosition - center) / Zoom;
        }

        public static Vector2 WorldToScreen(Vector2 worldPosition, int viewportWidth, int viewportHeight)
        {
            var center = new Vector2(viewportWidth / 2f, viewportHeight / 2f);
            return center + (worldPosition - center) * Zoom;
        }
    }
}
