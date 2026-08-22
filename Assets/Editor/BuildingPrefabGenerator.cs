#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NationBuilder.EditorTools
{
    /// <summary>
    /// One-time helper: wraps the handful of Fantasy Town Kit (Kenney, CC0) models
    /// that map 1:1 onto a building id into real prefabs under Resources/Buildings/,
    /// so BuildingWorldView (Assets/Scripts/World) has something to instantiate.
    /// Run Nation Builder > Generate Building Prefabs from the menu once.
    ///
    /// Most of the kit's 167 pieces are modular walls/roofs meant to be assembled by
    /// hand into distinct buildings - that's real level-design work best done inside
    /// the Editor, not guessed here. Buildings not listed below simply fall back to a
    /// placeholder cube in-game until someone assembles a proper model for them.
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        private const string ModelsFolder = "Assets/Art/FantasyTownKit/Models";
        private const string OutputFolder = "Assets/Resources/Buildings";

        private static readonly (string buildingId, string modelFileName)[] Mappings =
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

        [MenuItem("Nation Builder/Generate Building Prefabs (Fantasy Town Kit)")]
        public static void Generate()
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }

            int created = 0;
            foreach ((string buildingId, string modelFileName) in Mappings)
            {
                string modelPath = $"{ModelsFolder}/{modelFileName}";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    Debug.LogWarning($"모델을 찾을 수 없습니다: {modelPath}");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.SaveAsPrefabAsset(instance, $"{OutputFolder}/{buildingId}.prefab");
                Object.DestroyImmediate(instance);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"건물 프리팹 {created}개 생성 완료 ({OutputFolder}).");
        }
    }
}
#endif
