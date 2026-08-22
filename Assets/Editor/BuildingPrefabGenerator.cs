#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NationBuilder.EditorTools
{
    /// <summary>
    /// One-time helper: turns Fantasy Town Kit (Kenney, CC0) models into real prefabs
    /// so the game has something other than placeholder cubes to instantiate:
    ///  - Buildings that map 1:1 onto a single kit piece go to Resources/Buildings/
    ///    (read by BuildingWorldView).
    ///  - The town hall is assembled from a short vertical stack of pieces (base /
    ///    body / roof / banner), auto-aligned using each piece's actual mesh bounds
    ///    (no guessed offsets - reads real Renderer.bounds after instantiating each
    ///    one, so it self-corrects regardless of the kit's exact unit scale) and
    ///    saved to Resources/TownHall/town_hall.prefab (read by TownHallView).
    ///
    /// Run Nation Builder > Generate Building Prefabs from the menu once. Most of the
    /// kit's other 167 pieces are modular walls/roofs meant to be assembled by hand
    /// into distinct buildings - that's real level-design work best done inside the
    /// Editor, not guessed here. Anything not covered below simply falls back to a
    /// placeholder cube/primitive shape in-game.
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        private const string ModelsFolder = "Assets/Art/FantasyTownKit/Models";
        private const string BuildingsOutputFolder = "Assets/Resources/Buildings";
        private const string TownHallOutputFolder = "Assets/Resources/TownHall";
        private const float StackOverlap = 0.08f; // slight negative gap so seams don't show a hairline crack

        private static readonly (string buildingId, string modelFileName)[] BuildingMappings =
        {
            ("mill", "windmill.fbx"),
            ("market", "stall.fbx"),
            ("trade_post", "cart.fbx"),
            ("walls", "wall.fbx"),
            ("shrine", "fountain-round.fbx"),
            ("monument", "pillar-stone.fbx"),
            ("lumber_mill", "tree-high.fbx"),
            ("sawpit", "tree-crooked.fbx"),
        };

        // Bottom to top. wall-arch (grander than the plain "walls" building's wall.fbx)
        // + a tall gable roof + a banner on top reads as a distinct, prominent capital
        // building next to the smaller single-piece buildings above.
        private static readonly string[] TownHallStack =
        {
            "planks.fbx",
            "wall-arch.fbx",
            "roof-high-gable.fbx",
            "banner-red.fbx",
        };

        [MenuItem("Nation Builder/Generate Building Prefabs (Fantasy Town Kit)")]
        public static void Generate()
        {
            int buildings = GenerateBuildings();
            bool townHall = GenerateTownHall();

            Debug.Log($"건물 프리팹 {buildings}개, 마을회관 프리팹 {(townHall ? 1 : 0)}개 생성 완료.");
        }

        private static int GenerateBuildings()
        {
            EnsureFolder(BuildingsOutputFolder);

            int created = 0;
            foreach ((string buildingId, string modelFileName) in BuildingMappings)
            {
                GameObject model = LoadModel(modelFileName);
                if (model == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.SaveAsPrefabAsset(instance, $"{BuildingsOutputFolder}/{buildingId}.prefab");
                Object.DestroyImmediate(instance);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return created;
        }

        private static bool GenerateTownHall()
        {
            EnsureFolder(TownHallOutputFolder);

            var root = new GameObject("TownHallRoot");
            float nextBottomY = 0f;
            int placed = 0;

            foreach (string modelFileName in TownHallStack)
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

                // Center the piece horizontally, then drop its bottom exactly onto the
                // top of whatever was stacked before it.
                instance.transform.position += new Vector3(
                    -bounds.center.x,
                    nextBottomY - bounds.min.y,
                    -bounds.center.z);

                nextBottomY += bounds.size.y - StackOverlap;
                placed++;
            }

            if (placed == 0)
            {
                Object.DestroyImmediate(root);
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{TownHallOutputFolder}/town_hall.prefab");
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
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
    }
}
#endif
