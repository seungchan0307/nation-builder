using UnityEngine;

namespace NationBuilder.UI
{
    /// <summary>
    /// Generates a small plain-white rounded-rect bitmap at runtime (no image assets
    /// needed). Always white/opaque-alpha-mask - tint with GUI.backgroundColor for
    /// OnGUI or Image.color for uGUI, never GUI.color (that would tint text too).
    /// </summary>
    public static class RoundedTexture
    {
        public static Texture2D BuildTexture(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            int innerMin = radius;
            int innerMax = size - radius - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.Clamp(x, innerMin, innerMax);
                    int cy = Mathf.Clamp(y, innerMin, innerMax);
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx + 0.5f, cy + 0.5f));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        public static Sprite BuildSprite(int size, int radius)
        {
            Texture2D tex = BuildTexture(size, radius);
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }
    }
}
