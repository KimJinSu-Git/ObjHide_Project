using System.Linq;
using Bird.Network.Managers;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class GameStartUIHandler : MonoBehaviour
    {
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI playerCountText;

        private int _lastPlayerCount = -1;

        private void Start()
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void Update()
        {
            var gameManager = BirdGameManager.Instance;
            if (gameManager == null || gameManager.Runner == null || !gameManager.Runner.IsRunning) return;
            
            bool isLobby = gameManager.CurrentPhase == GamePhase.Lobby;
            
            if (readyPanel.activeSelf != isLobby)
            {
                readyPanel.SetActive(isLobby);
            }

            if (isLobby)
            {
                int currentPlayers = gameManager.PlayerDict.Count;
                
                if (_lastPlayerCount != currentPlayers)
                {
                    _lastPlayerCount = currentPlayers;
                    playerCountText.text = $"Player : {currentPlayers} / 10";
                }
                
                bool canStart = gameManager.Runner.IsServer && currentPlayers >= 2;
                if (startButton.interactable != canStart) startButton.interactable = canStart;
                if (startButton.gameObject.activeSelf != gameManager.Runner.IsServer) startButton.gameObject.SetActive(gameManager.Runner.IsServer);
            }
        }

        private void OnStartButtonClicked()
        {
            var gameManager = BirdGameManager.Instance;
            if (gameManager != null && gameManager.Runner != null && gameManager.Runner.IsServer)
            {
                gameManager.ManualStartGame();
            }
        }
    }
}
