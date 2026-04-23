using System;
using Bird.Network.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            BirdPlayerController.OnLocalHpChanged += UpdateHP;
            BirdPlayerController.OnLocalDeath += HideHUD;
        }

        private void OnDisable()
        {
            BirdPlayerController.OnLocalSpawned -= InitializeHUD;
            BirdPlayerController.OnLocalHpChanged -= UpdateHP;
            BirdPlayerController.OnLocalDeath -= HideHUD;
        }

        /// <summary>
        /// 체력 UI를 초기화하고 화면에 표시합니다.
        /// </summary>
        private void InitializeHUD(int currentHP)
        {
            hpObject.SetActive(true);
            UpdateHP(currentHP);
        }

        /// <summary>
        /// 현재 체력 수치에 맞춰 UI를 갱신합니다.
        /// </summary>
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
