using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering
{
    /// <summary>
    /// Slight world zoom used when drawing the map/characters so the view is not as close-up.
    /// Values below 1 zoom out; keep close to 1 so gameplay feel stays familiar.
    /// </summary>
    public static class WorldCamera
    {
        public const float Zoom = 0.88f;

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
