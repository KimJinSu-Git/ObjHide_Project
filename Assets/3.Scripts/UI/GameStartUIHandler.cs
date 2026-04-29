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

        private NetworkRunner _runner;
        
        private bool _isLobby;
        private int _currentPlayers;

        private void Start()
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void Update()
        {
            if (_runner == null)
            {
                _runner = FindObjectOfType<NetworkRunner>();
            }
            
            if (_runner == null || !_runner.IsRunning) return;
            
            _isLobby = BirdGameManager.Instance != null && BirdGameManager.Instance.CurrentPhase == GamePhase.Lobby;
            readyPanel.SetActive(_isLobby);

            if (_isLobby)
            {
                _currentPlayers = _runner.ActivePlayers.Count();
                playerCountText.text = $"Player : {_currentPlayers} / 10";
                
                startButton.interactable = _runner.IsServer && _currentPlayers >= 2;
                startButton.gameObject.SetActive(_runner.IsServer);
            }
        }

        private void OnStartButtonClicked()
        {
            if (_runner.IsServer && BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.ManualStartGame();
            }
        }
    }
}
