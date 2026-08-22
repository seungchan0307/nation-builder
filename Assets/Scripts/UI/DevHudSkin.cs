using NationBuilder.Core;
using UnityEngine;

namespace NationBuilder.UI
{
    /// <summary>
    /// Rounded-rect GUIStyles for the temporary OnGUI debug HUD, built once and cached.
    /// Panel/button backgrounds are plain white textures tinted per-draw-call with
    /// GUI.backgroundColor (never GUI.color, which would also wash out the text).
    /// </summary>
    public static class DevHudSkin
    {
        public static readonly Color Ink = new(0.95f, 0.95f, 0.92f);
        public static readonly Color Dim = new(0.78f, 0.78f, 0.75f);

        public static readonly Color PanelTint = new(0.09f, 0.10f, 0.14f, 0.94f);
        public static readonly Color ButtonTint = new(0.26f, 0.29f, 0.36f, 0.95f);
        public static readonly Color ButtonOnTint = new(0.30f, 0.55f, 0.85f, 0.95f);

        public static GUIStyle Panel { get; private set; }
        public static GUIStyle Chip { get; private set; }
        public static GUIStyle Header { get; private set; }
        public static GUIStyle SubHeader { get; private set; }
        public static GUIStyle Body { get; private set; }

        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            Texture2D panelTex = RoundedTexture.BuildTexture(64, 20);
            Texture2D chipTex = RoundedTexture.BuildTexture(28, 10);

            Panel = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex, textColor = Ink },
                border = new RectOffset(20, 20, 20, 20),
                padding = new RectOffset(14, 14, 12, 12),
                fontSize = 12,
            };

            Chip = new GUIStyle(GUI.skin.button)
            {
                normal = { background = chipTex, textColor = Ink },
                hover = { background = chipTex, textColor = Ink },
                active = { background = chipTex, textColor = Ink },
                border = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(3, 3, 3, 3),
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
            };

            Header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink },
            };

            SubHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Dim },
            };

            Body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Ink },
            };
        }

        public static Color CategoryColor(string category) => NationColors.ForCategory(category);
    }
}
