using System;
using Bird.Network.Managers;
using TMPro;
using UnityEngine;


namespace Bird.Network.UI
{
    public class GameStateUIHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI timerText;
        
        [SerializeField] private TextMeshProUGUI seekerCountText;
        [SerializeField] private TextMeshProUGUI hiderCountText;
        
        private void OnEnable()
        {
            BirdGameManager.OnPlayerCountChanged += UpdateStatus;
        }

        private void OnDisable()
        {
            BirdGameManager.OnPlayerCountChanged -= UpdateStatus;
        }

        private void Update()
        {
            if (BirdGameManager.Instance == null) return;
            if (!BirdGameManager.Instance.Object || !BirdGameManager.Instance.Object.IsValid) return;
            
            phaseText.text = $"{BirdGameManager.Instance.CurrentPhase}";

            if (BirdGameManager.Instance.StateTimer.IsRunning)
            {
                float? remainingTime = BirdGameManager.Instance.StateTimer.RemainingTime(BirdGameManager.Instance.Runner);
                timerText.text = remainingTime.HasValue ? $"{Mathf.CeilToInt(remainingTime.Value)} s" : "";
            }
        }
        
        private void UpdateStatus(int seekers, int hiders)
        {
            seekerCountText.text = $"{seekers}";
            hiderCountText.text = $"{hiders}";
        }
    }
}