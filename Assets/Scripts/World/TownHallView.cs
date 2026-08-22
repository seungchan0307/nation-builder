using System.Collections;
using UnityEngine;
using NationBuilder.Core;

namespace NationBuilder.World
{
    /// <summary>
    /// Visual for the town hall (capital) at the center of town. Built entirely from
    /// primitives (stone base / wood body / dark archway / stacked orange roof tiers
    /// / greenery), styled after a Clash-of-Clans-style tiered town hall reference the
    /// user provided - not from Fantasy Town Kit pieces, since matching a specific
    /// reference shape reliably needs full control over the geometry rather than
    /// guessing how kit pieces snap together.
    ///
    /// Swaps between three tiers as the town hall levels up (small hut -> arched hall
    /// -> grand hall with a beacon), so the building actually looks bigger/grander
    /// over time, not just uniformly scaled. Plays a light-flash + scale-pulse effect
    /// on every level-up.
    ///
    /// Everything lives under its own child transform (_root), not this component's
    /// own transform - this GameObject is the shared SystemsRoot (see GameBootstrap),
    /// which BuildingWorldView's building grid is also parented under, so scaling
    /// this component's own transform directly would have scaled every building in
    /// the world along with the town hall.
    /// </summary>
    public class TownHallView : MonoBehaviour
    {
        private static readonly Color StoneColor = new(0.56f, 0.56f, 0.6f);
        private static readonly Color WoodColor = new(0.62f, 0.46f, 0.3f);
        private static readonly Color RoofColor = new(0.86f, 0.56f, 0.16f);
        private static readonly Color DoorColor = new(0.22f, 0.14f, 0.1f);
        private static readonly Color LeafColor = new(0.28f, 0.5f, 0.24f);
        private static readonly Color BeaconColor = new(1f, 0.85f, 0.35f);
        private static readonly Color EffectColor = new(1f, 0.85f, 0.4f);

        private TownHallManager _townHall;
        private Transform _root;
        private GameObject _currentVisual;
        private int _currentTier = -1;

        public void Init(TownHallManager townHall)
        {
            _townHall = townHall;

            _root = new GameObject("TownHallRoot").transform;
            _root.SetParent(transform, false);

            ApplyVisualForLevel();
            ApplyScale();

            townHall.OnLevelUp += _ =>
            {
                ApplyVisualForLevel();
                ApplyScale();
                StartCoroutine(LevelUpEffect());
            };
        }

        private static int TierForLevel(int level)
        {
            if (level >= 6) return 3;
            if (level >= 3) return 2;
            return 1;
        }

        private void ApplyVisualForLevel()
        {
            int tier = TierForLevel(_townHall.Level);
            if (tier == _currentTier) return;
            _currentTier = tier;

            if (_currentVisual != null) Destroy(_currentVisual);

            _currentVisual = tier switch
            {
                1 => BuildTier1(),
                2 => BuildTier2(),
                _ => BuildTier3(),
            };
        }

        // Small wood hut, one modest roof tier, a couple of bushes.
        private GameObject BuildTier1()
        {
            GameObject root = NewVisualRoot();

            CreateBox(root.transform, "Base", new Vector3(0f, 0.1f, 0f), new Vector3(2.6f, 0.2f, 2.6f), StoneColor);
            CreateBox(root.transform, "Body", new Vector3(0f, 0.75f, 0f), new Vector3(2.0f, 1.1f, 2.0f), WoodColor);
            CreateBox(root.transform, "Door", new Vector3(0f, 0.35f, 1.02f), new Vector3(0.5f, 0.7f, 0.12f), DoorColor);
            BuildRoofTiers(root.transform, 1.3f, 1, 2.3f);
            ScatterGreenery(root.transform, 1.3f, 2);

            return root;
        }

        // Taller stone-and-wood hall, arched dark doorway, two stacked roof tiers.
        private GameObject BuildTier2()
        {
            GameObject root = NewVisualRoot();

            CreateBox(root.transform, "Base", new Vector3(0f, 0.12f, 0f), new Vector3(3.0f, 0.24f, 3.0f), StoneColor);
            CreateBox(root.transform, "Body", new Vector3(0f, 1.0f, 0f), new Vector3(2.3f, 1.6f, 2.3f), WoodColor);
            CreateBox(root.transform, "Door", new Vector3(0f, 0.5f, 1.18f), new Vector3(0.6f, 0.9f, 0.12f), DoorColor);
            BuildRoofTiers(root.transform, 1.85f, 2, 2.6f);
            ScatterGreenery(root.transform, 1.55f, 4);

            return root;
        }

        // Grand hall: widest base, three stacked roof tiers, gold beacon on top, lots of greenery.
        private GameObject BuildTier3()
        {
            GameObject root = NewVisualRoot();

            CreateBox(root.transform, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(3.6f, 0.3f, 3.6f), StoneColor);
            CreateBox(root.transform, "Body", new Vector3(0f, 1.3f, 0f), new Vector3(2.7f, 2.1f, 2.7f), WoodColor);
            CreateBox(root.transform, "Door", new Vector3(0f, 0.65f, 1.38f), new Vector3(0.7f, 1.1f, 0.12f), DoorColor);
            float roofTopY = BuildRoofTiers(root.transform, 2.5f, 3, 3.0f);
            CreatePrimitive(root.transform, PrimitiveType.Cylinder, "Beacon",
                new Vector3(0f, roofTopY + 0.25f, 0f), new Vector3(0.15f, 0.35f, 0.15f), BeaconColor);
            ScatterGreenery(root.transform, 1.9f, 6);

            return root;
        }

        private GameObject NewVisualRoot()
        {
            var root = new GameObject("TownHall_Visual");
            root.transform.SetParent(_root, false);
            return root;
        }

        /// <summary>Stacks `count` shrinking, 45-degree-rotated boxes (reads as a
        /// pyramidal/hip roof from most angles) like a tiered cake, mimicking the
        /// layered-roof look of the reference town hall. Returns the Y of the top of
        /// the topmost tier, for anything (e.g. a beacon) that needs to sit above it.</summary>
        private static float BuildRoofTiers(Transform parent, float startY, int count, float baseSize)
        {
            const float layerHeight = 0.55f;
            float y = startY;
            float size = baseSize;

            for (int i = 0; i < count; i++)
            {
                CreatePart(parent, PrimitiveType.Cube, $"Roof_{i}",
                    new Vector3(0f, y + layerHeight / 2f, 0f), Quaternion.Euler(0f, 45f, 0f),
                    new Vector3(size, layerHeight, size), RoofColor);
                y += layerHeight * 0.6f;
                size *= 0.75f;
            }

            return y;
        }

        private static void ScatterGreenery(Transform parent, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                Vector3 localPos = new(Mathf.Cos(angle) * radius, 0.12f, Mathf.Sin(angle) * radius);
                CreatePrimitive(parent, PrimitiveType.Sphere, "Greenery", localPos, Vector3.one * 0.35f, LeafColor);
            }
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            CreatePart(parent, PrimitiveType.Cube, name, localPosition, Quaternion.identity, localScale, color);
        }

        private static void CreatePrimitive(Transform parent, PrimitiveType primitive, string name,
            Vector3 localPosition, Vector3 localScale, Color color)
        {
            CreatePart(parent, primitive, name, localPosition, Quaternion.identity, localScale, color);
        }

        private static void CreatePart(Transform parent, PrimitiveType primitive, string name,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().material.color = color;
        }

        private void ApplyScale()
        {
            float growth = 1f + Mathf.Clamp(_townHall.Level - 1, 0, 10) * 0.05f;
            _root.localScale = Vector3.one * growth;
        }

        /// <summary>Built-in Light + a transform scale pulse - no custom shader/material
        /// needed, so it renders correctly no matter which render pipeline the project
        /// ends up using.</summary>
        private IEnumerator LevelUpEffect()
        {
            var flashObj = new GameObject("TownHall_LevelUpFlash");
            flashObj.transform.SetParent(_root, false);
            flashObj.transform.localPosition = Vector3.up * 2.5f;

            Light flash = flashObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = EffectColor;
            flash.range = 10f;

            const float duration = 0.9f;
            Vector3 baseScale = _root.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                flash.intensity = Mathf.Lerp(8f, 0f, t);
                _root.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f);
                yield return null;
            }

            _root.localScale = baseScale;
            Destroy(flashObj);
        }
    }
}
