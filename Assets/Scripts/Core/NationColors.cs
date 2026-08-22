using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>Single source of truth for the 경제/군사/기반/문화 accent colors, so the
    /// world view (placeholder building cubes) and the debug UI stay visually consistent.</summary>
    public static class NationColors
    {
        public static readonly Color Economy = new(0.95f, 0.85f, 0.3f);
        public static readonly Color Military = new(0.8f, 0.25f, 0.25f);
        public static readonly Color Infrastructure = new(0.6f, 0.6f, 0.65f);
        public static readonly Color Culture = new(0.35f, 0.55f, 0.9f);
        public static readonly Color Neutral = new(0.7f, 0.7f, 0.7f);

        public static Color ForCategory(string category) => category switch
        {
            "경제" => Economy,
            "군사" => Military,
            "기반" => Infrastructure,
            "문화" => Culture,
            _ => Neutral,
        };
    }
}
