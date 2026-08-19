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
        }

        private void OnEnable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += HandleGoldChanged;
                HandleGoldChanged(ResourceManager.Instance.Gold);
            }
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
