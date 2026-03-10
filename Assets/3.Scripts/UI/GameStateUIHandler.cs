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

        private void Update()
        {
            if (BirdGameManager.Instance == null) return;
            if (!BirdGameManager.Instance.Object || !BirdGameManager.Instance.Object.IsValid) return;
            
            phaseText.text = $"Current State : {BirdGameManager.Instance.CurrentPhase}";

            if (BirdGameManager.Instance.StateTimer.IsRunning)
            {
                float? remainingTime = BirdGameManager.Instance.StateTimer.RemainingTime(BirdGameManager.Instance.Runner);
                timerText.text = remainingTime.HasValue ? $"Remain Time : {Mathf.CeilToInt(remainingTime.Value)} Seconds" : "";
            }
        }
    }
}