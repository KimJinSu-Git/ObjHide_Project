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
                phaseText.text = GetPhaseKoreanName(_lastPhase);
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
        
        private string GetPhaseKoreanName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Lobby:   return "대기실";
                case GamePhase.Ready:   return "준비 시간";
                case GamePhase.Hide:    return "숨는 시간";
                case GamePhase.Reroll:  return "2차 선택 시간";
                case GamePhase.Final:   return "최종 시간";
                case GamePhase.Fever:   return "피버 타임!";
                case GamePhase.Result:  return "게임 종료";
                default:                return "알 수 없음";
            }
        }
    }
}