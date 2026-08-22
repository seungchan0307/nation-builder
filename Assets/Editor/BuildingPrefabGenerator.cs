#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NationBuilder.EditorTools
{
    /// <summary>
    /// One-time helper: turns Fantasy Town Kit (Kenney, CC0) models into real prefabs.
    ///  - SinglePieceMappings: a building that's already a complete standalone model
    ///    (windmill, stall, cart, fountain, ...) - maps 1:1, no assembly needed.
    ///  - BuildingRoofs: a roof (+ optional topper prop) for buildings that are just a
    ///    primitive wall box at runtime (see BuildingWorldView) topped with real roof
    ///    art. NOT full building assemblies - Kenney's "wall" pieces are single flat
    ///    panels (one side of a box), and 4 of them only close into a real box if you
    ///    know the piece's pivot convention, which can't be verified without opening
    ///    the piece in the Editor. Walls are primitives instead so they're guaranteed
    ///    to look solid from every angle; only the roof needs a real model, and a roof
    ///    cap is a single complete piece so there's no assembly ambiguity there.
    ///
    /// Saved to Assets/Resources/Buildings/{id}.prefab (single-piece) or
    /// Assets/Resources/Buildings/{id}_roof.prefab (roof-only). Run Nation Builder >
    /// Generate Building Prefabs from the menu once (re-run after editing the lists
    /// below to regenerate). The town hall does NOT use this - see TownHallView,
    /// which builds it entirely from primitives to match a specific reference image.
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        private const string ModelsFolder = "Assets/Art/FantasyTownKit/Models";
        private const string BuildingsOutputFolder = "Assets/Resources/Buildings";
        private const float StackOverlap = 0.08f; // slight negative gap so seams don't show a hairline crack

        private static readonly (string buildingId, string modelFileName)[] SinglePieceMappings =
        {
            ("mill", "windmill.fbx"),
            ("market", "stall.fbx"),
            ("trade_post", "cart.fbx"),
            ("walls", "wall.fbx"),
            ("shrine", "fountain-round.fbx"),
            ("monument", "pillar-stone.fbx"),
            ("lumber_mill", "tree-high.fbx"),
            ("sawpit", "tree-crooked.fbx"),
            ("road", "road.fbx"),
            ("amphitheater", "stairs-full.fbx"),
            ("aqueduct", "watermill-wide.fbx"),
        };

        // Roof (+ optional topper) only - BuildingWorldView builds the wall box itself
        // and stacks this on top at runtime using the same bounds-measurement approach.
        private static readonly (string buildingId, string[] pieces)[] BuildingRoofs =
        {
            ("farm", new[] { "roof-gable.fbx" }),
            ("granary", new[] { "roof-gable.fbx" }),
            ("bank", new[] { "roof-flat.fbx", "chimney.fbx" }),
            ("barracks", new[] { "roof-flat.fbx", "blade.fbx" }),
            ("archery_range", new[] { "roof-flat.fbx" }),
            ("fortress", new[] { "roof-high.fbx", "pillar-stone.fbx" }),
            ("war_camp", new[] { "banner-green.fbx" }),
            ("quarry", new[] { "roof-flat.fbx", "rock-large.fbx" }),
            ("workshop", new[] { "roof-flat.fbx", "wheel.fbx" }),
            ("library", new[] { "roof-gable.fbx", "lantern.fbx" }),
            ("observatory", new[] { "roof-high.fbx", "lantern.fbx" }),
            ("grand_hall", new[] { "roof-high.fbx", "banner-green.fbx" }),
        };

        [MenuItem("Nation Builder/Generate Building Prefabs (Fantasy Town Kit)")]
        public static void Generate()
        {
            EnsureFolder(BuildingsOutputFolder);
            RemoveStaleAsset("Assets/Resources/TownHall/town_hall.prefab");
            RemoveStaleAsset("Assets/Resources/TownHall/town_hall_tier1.prefab");
            RemoveStaleAsset("Assets/Resources/TownHall/town_hall_tier2.prefab");
            RemoveStaleAsset("Assets/Resources/TownHall/town_hall_tier3.prefab");

            // These used to be full assemblies (base+wall+roof) saved as {id}.prefab
            // directly - now BuildingWorldView builds the wall box itself and only
            // loads {id}_roof.prefab on top. A stale {id}.prefab from an older run
            // would otherwise take priority and silently keep showing the old
            // single-flat-wall-panel look.
            foreach ((string buildingId, _) in BuildingRoofs)
            {
                RemoveStaleAsset($"{BuildingsOutputFolder}/{buildingId}.prefab");
            }

            int singlePiece = GenerateSinglePieceBuildings();
            int roofs = GenerateRoofPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"완성 모델 {singlePiece}개, 지붕 프리팹 {roofs}개 생성 완료.");
        }

        private static int GenerateSinglePieceBuildings()
        {
            int created = 0;
            foreach ((string buildingId, string modelFileName) in SinglePieceMappings)
            {
                GameObject model = LoadModel(modelFileName);
                if (model == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.SaveAsPrefabAsset(instance, $"{BuildingsOutputFolder}/{buildingId}.prefab");
                Object.DestroyImmediate(instance);
                created++;
            }
            return created;
        }

        private static int GenerateRoofPrefabs()
        {
            int created = 0;
            foreach ((string buildingId, string[] pieces) in BuildingRoofs)
            {
                GameObject root = BuildStackRoot($"{buildingId}_roof", pieces);
                if (root == null) continue;

                PrefabUtility.SaveAsPrefabAsset(root, $"{BuildingsOutputFolder}/{buildingId}_roof.prefab");
                Object.DestroyImmediate(root);
                created++;
            }
            return created;
        }

        /// <summary>Instantiates each piece bottom-to-top, centers it horizontally on its
        /// own bounds, and sits its bottom exactly on top of whatever came before -
        /// all measured from real Renderer.bounds so pieces never gap or overlap
        /// regardless of their individual size/pivot.</summary>
        private static GameObject BuildStackRoot(string rootName, string[] pieces)
        {
            var root = new GameObject(rootName);
            float nextBottomY = 0f;
            int placed = 0;

            foreach (string modelFileName in pieces)
            {
                GameObject model = LoadModel(modelFileName);
                if (model == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.transform.SetParent(root.transform, true);

                Bounds bounds = GetRendererBounds(instance);
                if (bounds.size == Vector3.zero)
                {
                    Debug.LogWarning($"{modelFileName}에 렌더러가 없어서 건너뜁니다.");
                    Object.DestroyImmediate(instance);
                    continue;
                }

                instance.transform.position += new Vector3(
                    -bounds.center.x,
                    nextBottomY - bounds.min.y,
                    -bounds.center.z);

                nextBottomY += bounds.size.y - StackOverlap;
                placed++;
            }

            if (placed > 0) return root;

            Object.DestroyImmediate(root);
            return null;
        }

        private static GameObject LoadModel(string modelFileName)
        {
            string modelPath = $"{ModelsFolder}/{modelFileName}";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"모델을 찾을 수 없습니다: {modelPath}");
            }
            return model;
        }

        private static Bounds GetRendererBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        private static void RemoveStaleAsset(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }
}
#endif
