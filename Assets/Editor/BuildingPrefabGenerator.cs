#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NationBuilder.EditorTools
{
    /// <summary>
    /// One-time helper: turns Fantasy Town Kit (Kenney, CC0) models into real prefabs
    /// so the game has something other than placeholder cubes to instantiate:
    ///  - SinglePieceMappings: a building that maps 1:1 onto one kit piece.
    ///  - BuildingStacks: a building assembled from a short vertical stack of pieces
    ///    (e.g. base / wall / roof / topper). Alignment is computed from each piece's
    ///    actual Renderer.bounds after instantiating it - not guessed offsets - so it
    ///    self-corrects regardless of the kit's exact unit scale.
    ///  - TownHallTiers: three town_hall_tier{1,2,3} prefabs (small -> grand), read by
    ///    TownHallView and swapped in as the town hall levels up.
    ///
    /// All of the above land under Assets/Resources/{Buildings,TownHall}/. Run
    /// Nation Builder > Generate Building Prefabs from the menu once (re-run any time
    /// after editing the lists below to regenerate).
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        private const string ModelsFolder = "Assets/Art/FantasyTownKit/Models";
        private const string BuildingsOutputFolder = "Assets/Resources/Buildings";
        private const string TownHallOutputFolder = "Assets/Resources/TownHall";
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

        private static readonly (string buildingId, string[] pieces)[] BuildingStacks =
        {
            ("farm", new[] { "planks.fbx", "wall-wood-door.fbx", "roof-gable.fbx" }),
            ("granary", new[] { "planks.fbx", "wall-block.fbx", "roof-gable.fbx" }),
            ("bank", new[] { "planks.fbx", "wall-block.fbx", "roof-flat.fbx", "chimney.fbx" }),
            ("barracks", new[] { "planks.fbx", "wall-wood-door.fbx", "roof-flat.fbx", "blade.fbx" }),
            ("archery_range", new[] { "planks.fbx", "wall-wood.fbx", "roof-flat.fbx" }),
            ("fortress", new[] { "planks.fbx", "wall-block.fbx", "wall-block.fbx", "pillar-stone.fbx" }),
            ("war_camp", new[] { "planks.fbx", "fence.fbx", "banner-green.fbx" }),
            ("quarry", new[] { "planks.fbx", "wall-block.fbx", "roof-flat.fbx", "rock-large.fbx" }),
            ("workshop", new[] { "planks.fbx", "wall-wood-window-shutters.fbx", "roof-flat.fbx", "wheel.fbx" }),
            ("library", new[] { "planks.fbx", "wall-wood-window-glass.fbx", "roof-gable.fbx", "lantern.fbx" }),
            ("observatory", new[] { "planks.fbx", "wall-block.fbx", "roof-high.fbx", "lantern.fbx" }),
            ("grand_hall", new[] { "planks.fbx", "wall-arch.fbx", "roof-high.fbx", "banner-green.fbx" }),
        };

        // Small -> grand. TownHallView swaps between these as the town hall levels up.
        private static readonly (string tierId, string[] pieces)[] TownHallTiers =
        {
            ("town_hall_tier1", new[] { "planks.fbx", "wall-wood-door.fbx", "roof-gable.fbx" }),
            ("town_hall_tier2", new[] { "planks.fbx", "wall-arch.fbx", "roof-high-gable.fbx", "banner-red.fbx" }),
            ("town_hall_tier3", new[]
                { "planks.fbx", "wall-arch.fbx", "wall-block.fbx", "roof-high-gable.fbx", "banner-red.fbx" }),
        };

        [MenuItem("Nation Builder/Generate Building Prefabs (Fantasy Town Kit)")]
        public static void Generate()
        {
            EnsureFolder(BuildingsOutputFolder);
            EnsureFolder(TownHallOutputFolder);
            RemoveStaleAsset($"{TownHallOutputFolder}/town_hall.prefab"); // superseded by the tiered prefabs below

            int singlePiece = GenerateSinglePieceBuildings();
            int stacked = GenerateStackedPrefabs(BuildingsOutputFolder, BuildingStacks);
            int townHallTiers = GenerateStackedPrefabs(TownHallOutputFolder, TownHallTiers);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"건물 프리팹 {singlePiece + stacked}개, 마을회관 단계 {townHallTiers}개 생성 완료.");
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

        private static int GenerateStackedPrefabs(string outputFolder, (string id, string[] pieces)[] definitions)
        {
            int created = 0;
            foreach ((string id, string[] pieces) in definitions)
            {
                GameObject root = BuildStackRoot(id, pieces);
                if (root == null) continue;

                PrefabUtility.SaveAsPrefabAsset(root, $"{outputFolder}/{id}.prefab");
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
