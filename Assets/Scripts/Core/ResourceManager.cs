using System;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Accumulates gold in real time while the app is running.
    /// Offline progress (accumulating while the app is closed) is intentionally
    /// not implemented yet - that is a later increment.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private double goldPerSecond = 1.0;
        [SerializeField] private double startingGold = 0.0;

        public double Gold { get; private set; }

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
    }
}
