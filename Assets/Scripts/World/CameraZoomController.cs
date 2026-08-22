using UnityEngine;

namespace NationBuilder.World
{
    /// <summary>
    /// Mouse-wheel zoom for the isometric-style main camera. Moves along the camera's
    /// own forward axis (dolly) instead of changing field of view, so the fixed tilt
    /// angle that makes it read as "isometric" is preserved rather than distorted.
    /// Clamped by height so it can't dolly through the ground or off into the sky.
    /// </summary>
    public class CameraZoomController : MonoBehaviour
    {
        [SerializeField] private float zoomSpeed = 1.2f;
        [SerializeField] private float minHeight = 4f;
        [SerializeField] private float maxHeight = 26f;

        private void Update()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            Vector3 next = transform.position + transform.forward * (scroll * zoomSpeed);
            if (next.y < minHeight || next.y > maxHeight) return;

            transform.position = next;
        }
    }
}
