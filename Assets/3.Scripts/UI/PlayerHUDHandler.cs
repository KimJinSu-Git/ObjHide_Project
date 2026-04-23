using Bird.Network.Player;
using TMPro;
using UnityEngine;

namespace Bird.Network.UI
{
    public class PlayerHUDHandler : MonoBehaviour
    {
        [Header("Health UI")] 
        [SerializeField] private GameObject hpObject;
        [SerializeField] private TextMeshProUGUI hpText;

        private void Awake()
        {
            hpObject.SetActive(false);
        }

        private void OnEnable()
        {
            BirdPlayerController.OnLocalSpawned += InitializeHUD;
            BirdPlayerHealth.OnLocalHpChanged += UpdateHP;
            BirdPlayerHealth.OnLocalDeath += HideHUD;
        }

        private void OnDisable()
        {
            BirdPlayerController.OnLocalSpawned -= InitializeHUD;
            BirdPlayerHealth.OnLocalHpChanged -= UpdateHP;
            BirdPlayerHealth.OnLocalDeath -= HideHUD;
        }

        private void InitializeHUD(int currentHP)
        {
            hpObject.SetActive(true);
            UpdateHP(currentHP);
        }

        private void UpdateHP(int currentHP)
        {
            if (hpText != null)
            {
                hpText.text = $"HP : {currentHP}";
            }
        }

        private void HideHUD()
        {
            hpObject.SetActive(false);
        }
    }
}
