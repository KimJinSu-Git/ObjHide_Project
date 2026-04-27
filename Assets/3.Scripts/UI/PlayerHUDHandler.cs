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
            BirdPlayerHealth.OnLocalHpChanged += UpdateHp;
            BirdPlayerHealth.OnLocalDeath += HideHUD;
        }

        private void OnDisable()
        {
            BirdPlayerController.OnLocalSpawned -= InitializeHUD;
            BirdPlayerHealth.OnLocalHpChanged -= UpdateHp;
            BirdPlayerHealth.OnLocalDeath -= HideHUD;
        }

        private void InitializeHUD(int currentHp)
        {
            hpObject.SetActive(true);
            UpdateHp(currentHp);
        }

        private void UpdateHp(int currentHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP : {currentHp}";
            }
        }

        private void HideHUD()
        {
            hpObject.SetActive(false);
        }
    }
}
