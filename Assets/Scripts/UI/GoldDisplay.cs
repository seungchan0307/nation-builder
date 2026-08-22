using NationBuilder.Core;
using TMPro;
using UnityEngine;

namespace NationBuilder.UI
{
    /// <summary>
    /// Shows the current gold amount, rounded down to a whole number.
    /// Attach to a UI GameObject with a TextMeshProUGUI component.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GoldDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            PositionTopRight();
        }

        /// <summary>Pins this label to the screen's top-right corner regardless of how it
        /// was placed in the scene, so it doesn't need to be moved by hand in the Editor.</summary>
        private void PositionTopRight()
        {
            if (transform is not RectTransform rect) return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            _label.alignment = TextAlignmentOptions.TopRight;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            // OnEnable can run before ResourceManager.Awake() sets Instance
            // (execution order between different GameObjects isn't guaranteed).
            // Start() is guaranteed to run after every Awake() in the scene,
            // so retry here in case the OnEnable subscribe attempt was a no-op.
            Subscribe();
        }

        private void Subscribe()
        {
            if (ResourceManager.Instance == null) return;
            ResourceManager.Instance.OnGoldChanged -= HandleGoldChanged;
            ResourceManager.Instance.OnGoldChanged += HandleGoldChanged;
            HandleGoldChanged(ResourceManager.Instance.Gold);
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void HandleGoldChanged(double gold)
        {
            _label.text = $"Gold: {Mathf.FloorToInt((float)gold)}";
        }
    }
}
