using Fusion;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class InGameMenuUIHandler : MonoBehaviour
    {
        [Header("UI Panels")] 
        [SerializeField] private GameObject menuPanel;

        [Header("Buttons")] 
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        private NetworkRunner _runner;

        private void Awake()
        {
            menuPanel.SetActive(false);
            
            resumeButton.onClick.AddListener(CloseMenu);
            quitButton.onClick.AddListener(LeaveGame);
        }

        private void Update()
        {
            if (_runner == null) 
            {
                _runner = FindObjectOfType<NetworkRunner>();
            }
        }

        public void OpenMenu()
        {
            menuPanel.SetActive(true);
        }

        public void ToggleMenu()
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }

        private void CloseMenu()
        {
            menuPanel.SetActive(false);
        }

        /// <summary>
        /// 게임 나가기 버튼 클릭 시 호출
        /// </summary>
        private void LeaveGame()
        {
            quitButton.interactable = false; // 중복 클릭 방지

            if (_runner != null && _runner.IsRunning)
            {
                _runner.Shutdown();
            }
        }
    }
}