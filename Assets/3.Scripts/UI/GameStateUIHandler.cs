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

        private GamePhase _lastPhase = (GamePhase)(-1);
        private int _lastTime = -1;
        
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
            var gameManager = BirdGameManager.Instance;
            if (gameManager == null || !gameManager.Object || !gameManager.Object.IsValid) return;

            if (_lastPhase != gameManager.CurrentPhase)
            {
                _lastPhase = gameManager.CurrentPhase;
                phaseText.text = _lastPhase.ToString();
            }

            if (gameManager.StateTimer.IsRunning)
            {
                float? remainingTime = gameManager.StateTimer.RemainingTime(gameManager.Runner);
                if (remainingTime.HasValue)
                {
                    int currentSeconds = Mathf.CeilToInt(remainingTime.Value);

                    if (_lastTime != currentSeconds)
                    {
                        _lastTime = currentSeconds;
                        timerText.text = $"{currentSeconds} s";
                    }
                }
            }
            else if (_lastTime != 0)
            {
                _lastTime = 0;
                timerText.text = "";
            }
        }
        
        private void UpdateStatus(int seekers, int hiders)
        {
            seekerCountText.text = $"{seekers}";
            hiderCountText.text = $"{hiders}";
        }
    }
}