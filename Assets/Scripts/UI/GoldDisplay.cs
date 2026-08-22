using NationBuilder.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NationBuilder.UI
{
    /// <summary>
    /// Shows the current gold amount, rounded down to a whole number.
    /// Attach to a UI GameObject with a TextMeshProUGUI component.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GoldDisplay : MonoBehaviour
    {
        private static readonly Color GoldTextColor = new(1f, 0.85f, 0.4f);
        private static readonly Color BadgeColor = new(0.09f, 0.10f, 0.14f, 0.85f);

        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            PositionTopRight();
            AddBackgroundBadge();
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
            _label.color = GoldTextColor;
            _label.fontStyle = FontStyles.Bold;
        }

        /// <summary>Drops a rounded, semi-transparent panel behind this label so it reads
        /// as a HUD "chip" instead of floating bare text over the 3D scene.</summary>
        private void AddBackgroundBadge()
        {
            if (transform is not RectTransform textRect) return;

            var badge = new GameObject("GoldBadgeBackground", typeof(RectTransform), typeof(Image));
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.SetParent(textRect.parent, false);
            badgeRect.SetSiblingIndex(textRect.GetSiblingIndex()); // sits right before the text -> renders behind it

            const float padX = 18f;
            const float padY = 12f;
            badgeRect.anchorMin = textRect.anchorMin;
            badgeRect.anchorMax = textRect.anchorMax;
            badgeRect.pivot = textRect.pivot;
            badgeRect.anchoredPosition = textRect.anchoredPosition + new Vector2(padX, padY);
            badgeRect.sizeDelta = textRect.sizeDelta + new Vector2(padX * 2f, padY * 2f);

            var image = badge.GetComponent<Image>();
            image.sprite = RoundedTexture.BuildSprite(48, 18);
            image.type = Image.Type.Sliced;
            image.color = BadgeColor;
            image.raycastTarget = false;
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
