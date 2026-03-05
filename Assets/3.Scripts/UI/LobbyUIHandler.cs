using Bird.Network.Handlers;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    /// <summary>
    /// 메인 로비 UI 요소 제어
    /// </summary>
    public class LobbyUIHandler : MonoBehaviour
    {
        [SerializeField] private BirdNetworkHandler networkHandler;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;

        private void Awake()
        {
            hostButton.onClick.AddListener(() => networkHandler.StartGame(GameMode.Host));
            joinButton.onClick.AddListener(() => networkHandler.StartGame(GameMode.Client));
        }
    }
}
