using UnityEngine;

namespace NationBuilder.World
{
    /// <summary>
    /// One-shot scene dressing so the town doesn't render into an empty void: a large
    /// flat ground plane, and a directional light as a fallback if the scene doesn't
    /// already have one. Pure primitives/code, no scene editing required.
    /// </summary>
    public static class WorldDressing
    {
        public static void Setup()
        {
            CreateGround();
            EnsureLight();
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            // Unity's default Plane primitive is 10x10 units, so this scale covers ~100x100.
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.55f, 0.32f);
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null) return;

            var lightObj = new GameObject("Directional Light (auto)");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
