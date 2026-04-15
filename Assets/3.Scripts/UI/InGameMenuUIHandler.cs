using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
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

        private NetworkRunner runner;

        private void Awake()
        {
            menuPanel.SetActive(false);
            
            resumeButton.onClick.AddListener(CloseMenu);
            quitButton.onClick.AddListener(LeaveGame);
        }

        private void Start()
        {
            runner = FindObjectOfType<NetworkRunner>();
        }

        public void OpenMenu()
        {
            menuPanel.SetActive(true);
        }

        public void CloseMenu()
        {
            menuPanel.SetActive(false);
        }

        /// <summary>
        /// 게임 나가기 버튼 클릭 시 호출
        /// </summary>
        private async void LeaveGame()
        {
            quitButton.interactable = false; // 중복 클릭 방지

            if (runner != null && runner.IsRunning)
            {
                await runner.Shutdown();
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }
}