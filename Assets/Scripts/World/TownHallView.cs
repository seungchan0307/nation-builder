using System.Collections;
using UnityEngine;
using NationBuilder.Core;

namespace NationBuilder.World
{
    /// <summary>
    /// Visual for the town hall (capital) at the center of town. Swaps between three
    /// Resources/TownHall/town_hall_tier{1,2,3}.prefab models (built from Fantasy Town
    /// Kit pieces by BuildingPrefabGenerator, Editor-only) as the town hall levels up,
    /// so the building actually looks bigger/grander over time, not just scaled up.
    /// Falls back to a primitive placeholder if a tier's prefab hasn't been generated
    /// yet, so there's still something to look at with no Editor step required.
    /// Plays a light-flash + scale-pulse effect on every level-up.
    ///
    /// Everything lives under its own child transform (_root), not this component's
    /// own transform - this GameObject is the shared SystemsRoot (see GameBootstrap),
    /// which BuildingWorldView's building grid is also parented under, so scaling
    /// this component's own transform directly would have scaled every building
    /// in the world along with the town hall.
    /// </summary>
    public class TownHallView : MonoBehaviour
    {
        private static readonly Color BaseColor = new(0.5f, 0.5f, 0.52f);
        private static readonly Color WallColor = new(0.78f, 0.72f, 0.6f);
        private static readonly Color RoofColor = new(0.55f, 0.2f, 0.18f);
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

            GameObject prefab = Resources.Load<GameObject>($"TownHall/town_hall_tier{tier}");
            _currentVisual = prefab != null ? Instantiate(prefab, _root, false) : BuildPlaceholder();
        }

        private GameObject BuildPlaceholder()
        {
            var container = new GameObject("TownHall_Placeholder");
            container.transform.SetParent(_root, false);

            CreatePart(container.transform, PrimitiveType.Cube, "TownHall_Base",
                new Vector3(0f, 0.15f, 0f), Quaternion.identity, new Vector3(3.2f, 0.3f, 3.2f), BaseColor);

            CreatePart(container.transform, PrimitiveType.Cube, "TownHall_Body",
                new Vector3(0f, 1.1f, 0f), Quaternion.identity, new Vector3(2.4f, 1.8f, 2.4f), WallColor);

            CreatePart(container.transform, PrimitiveType.Cube, "TownHall_Roof",
                new Vector3(0f, 2.35f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(1.9f, 0.6f, 1.9f), RoofColor);

            CreatePart(container.transform, PrimitiveType.Cylinder, "TownHall_Beacon",
                new Vector3(0f, 3.1f, 0f), Quaternion.identity, new Vector3(0.15f, 0.4f, 0.15f), BeaconColor);

            return container;
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
