using UnityEngine;
using NationBuilder.Core;

namespace NationBuilder.World
{
    /// <summary>
    /// Spawns a simple visual in the world for each placed building: a real model
    /// from Resources/Buildings/{id}.prefab if one has been generated for that id
    /// yet (see BuildingPrefabGenerator, Editor-only), otherwise a placeholder cube
    /// tinted by the building's node category (경제/군사/기반/문화) until real art
    /// exists for it. Purely additive - BuildingManager/save data don't know this
    /// view exists, so it's safe to delete or replace wholesale later.
    /// </summary>
    public class BuildingWorldView : MonoBehaviour
    {
        private const float GridSpacing = 4f;
        private const int GridColumns = 5;

        // Buildings lay out in front of the town hall (which sits at the world
        // origin, see TownHallView) instead of starting right on top of it.
        private const float RowStartOffset = 6f;

        private NodeTreeManager _nodeTree;
        private Transform _root;
        private int _spawnedCount;

        public void Init(BuildingManager buildingManager, NodeTreeManager nodeTree)
        {
            _nodeTree = nodeTree;

            _root = new GameObject("BuildingsRoot").transform;
            _root.SetParent(transform, false);

            buildingManager.OnBuildingPlaced += SpawnFor;

            // Buildings restored from a save are already in Placed before we subscribed above.
            foreach (PlacedBuilding placed in buildingManager.Placed)
            {
                SpawnFor(placed);
            }
        }

        private void SpawnFor(PlacedBuilding placed)
        {
            Vector3 position = GridPosition(_spawnedCount);
            _spawnedCount++;

            GameObject prefab = Resources.Load<GameObject>($"Buildings/{placed.BuildingId}");
            GameObject instance = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity, _root)
                : CreatePlaceholder(placed.BuildingId, position);

            instance.name = placed.BuildingId;
        }

        private GameObject CreatePlaceholder(string buildingId, Vector3 position)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(_root, false);
            cube.transform.position = position + Vector3.up * 0.5f;
            cube.GetComponent<Renderer>().material.color = NationColors.ForCategory(FindCategory(buildingId));
            return cube;
        }

        private static Vector3 GridPosition(int index)
        {
            int col = index % GridColumns;
            int row = index / GridColumns;
            float x = (col - (GridColumns - 1) / 2f) * GridSpacing;
            float z = RowStartOffset + row * GridSpacing;
            return new Vector3(x, 0f, z);
        }

        private string FindCategory(string buildingId)
        {
            foreach (TreeNode node in _nodeTree.AllNodes)
            {
                if (node.UnlocksBuildingId == buildingId) return node.Category;
            }
            return null;
        }
    }
}
