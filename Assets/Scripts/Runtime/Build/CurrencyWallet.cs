using System;
using UnityEngine;

namespace Runtime.Build
{
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingCoins = 1000;

        public event Action<int> OnCoinsChanged;

        public int Coins { get; private set; }

        private void Awake()
        {
            Coins = Mathf.Max(0, startingCoins);
            OnCoinsChanged?.Invoke(Coins);
        }

        public void SetCoins(int value)
        {
            Coins = Mathf.Max(0, value);
            OnCoinsChanged?.Invoke(Coins);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            OnCoinsChanged?.Invoke(Coins);
        }
    }
}

