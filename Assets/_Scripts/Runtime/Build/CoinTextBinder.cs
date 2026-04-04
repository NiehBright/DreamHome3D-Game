using TMPro;
using UnityEngine;

namespace Runtime.Build
{
    public class CoinTextBinder : MonoBehaviour
    {
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private string prefix = "Coin: ";

        private void Awake()
        {
            if (wallet == null)
            {
                wallet = FindFirstObjectByType<CurrencyWallet>();
            }
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.OnCoinsChanged += HandleCoinsChanged;
                HandleCoinsChanged(wallet.Coins);
            }
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.OnCoinsChanged -= HandleCoinsChanged;
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            if (coinText != null)
            {
                coinText.text = prefix + coins;
            }
        }
    }
}

