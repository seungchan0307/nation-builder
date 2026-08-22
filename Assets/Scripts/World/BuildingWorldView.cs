using UnityEngine;
using NationBuilder.Core;

namespace NationBuilder.World
{
    /// <summary>
    /// Spawns a simple visual in the world for each placed building:
    ///  - If Resources/Buildings/{id}.prefab exists, it's already a complete
    ///    standalone model (windmill, stall, cart, ...) - instantiated as-is.
    ///  - Otherwise, builds a solid primitive wall box (tinted by the building's
    ///    node category - 경제/군사/기반/문화) so there's always a real enclosed
    ///    building shape, then stacks Resources/Buildings/{id}_roof.prefab on top
    ///    if one has been generated (real Kenney roof art; falls back to just the
    ///    box if not). Roof alignment is measured from actual Renderer.bounds at
    ///    runtime, same technique BuildingPrefabGenerator uses in the Editor.
    ///
    /// Purely additive - BuildingManager/save data don't know this view exists,
    /// so it's safe to delete or replace wholesale later.
    /// </summary>
    public class BuildingWorldView : MonoBehaviour
    {
        private static readonly Color WallBaseColor = new(0.55f, 0.55f, 0.55f);

        private const float GridSpacing = 4f;
        private const int GridColumns = 5;

        // Buildings lay out in front of the town hall (which sits at the world
        // origin, see TownHallView) instead of starting right on top of it.
        private const float RowStartOffset = 6f;

        private const float WallBaseHeight = 0.2f;
        private const float WallHeight = 1.4f;
        private const float WallTopY = WallBaseHeight + WallHeight;

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

            GameObject completePrefab = Resources.Load<GameObject>($"Buildings/{placed.BuildingId}");
            GameObject instance = completePrefab != null
                ? Instantiate(completePrefab, position, Quaternion.identity, _root)
                : BuildWallBox(placed.BuildingId, position);

            instance.name = placed.BuildingId;
        }

        private GameObject BuildWallBox(string buildingId, Vector3 position)
        {
            var container = new GameObject(buildingId);
            container.transform.SetParent(_root, false);
            container.transform.position = position;

            Color tint = NationColors.ForCategory(FindCategory(buildingId));

            CreatePrimitiveChild(container.transform, PrimitiveType.Cube,
                new Vector3(0f, WallBaseHeight / 2f, 0f), new Vector3(1.8f, WallBaseHeight, 1.8f), WallBaseColor);

            CreatePrimitiveChild(container.transform, PrimitiveType.Cube,
                new Vector3(0f, WallBaseHeight + WallHeight / 2f, 0f), new Vector3(1.4f, WallHeight, 1.4f), tint);

            GameObject roofPrefab = Resources.Load<GameObject>($"Buildings/{buildingId}_roof");
            if (roofPrefab != null)
            {
                GameObject roofInstance = Instantiate(roofPrefab, container.transform, false);
                PlaceOnTop(container.transform, roofInstance, WallTopY);
            }

            return container;
        }

        private static void CreatePrimitiveChild(Transform parent, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().material.color = color;
        }

        /// <summary>Centers `instance` horizontally on `container` and drops its bottom
        /// exactly onto `localBottomY` (measured in container's local space) - using
        /// the instance's real Renderer.bounds, not a guessed offset.</summary>
        private static void PlaceOnTop(Transform container, GameObject instance, float localBottomY)
        {
            Bounds bounds = GetRendererBounds(instance);
            if (bounds.size == Vector3.zero) return;

            Vector3 targetBottom = container.position + new Vector3(0f, localBottomY, 0f);
            instance.transform.position += new Vector3(
                container.position.x - bounds.center.x,
                targetBottom.y - bounds.min.y,
                container.position.z - bounds.center.z);
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
