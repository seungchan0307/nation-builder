using System;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Accumulates gold in real time while the app is running.
    /// Offline progress is computed by GameBootstrap/SaveSystem on load,
    /// which calls SetGold() once it has added the offline amount.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private double goldPerSecond = 1.0;
        [SerializeField] private double startingGold = 0.0;

        public double Gold { get; private set; }
        public double GoldPerSecond => goldPerSecond;

        public event Action<double> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Gold = startingGold;
        }

        private void Update()
        {
            AddGold(goldPerSecond * Time.deltaTime);
        }

        public void AddGold(double amount)
        {
            if (amount == 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpendGold(double amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        /// <summary>Used by SaveSystem to restore a saved amount (including offline earnings).</summary>
        public void SetGold(double amount)
        {
            Gold = amount;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>Used by milestone choices that specialize the nation toward economy.</summary>
        public void MultiplyGoldRate(double factor)
        {
            goldPerSecond *= factor;
        }
    }
}
