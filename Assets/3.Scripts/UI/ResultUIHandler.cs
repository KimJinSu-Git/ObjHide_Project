using System.Collections;
using Bird.Network.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using UnityEngine.SceneManagement;

namespace Bird.Network.UI
{
    public class ResultUIHandler : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI winTitleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button lobbyButton;

        private void Awake()
        {
            resultPanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0;
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
        }

        private IEnumerator Start()
        {
            while (BirdGameManager.Instance == null)
            {
                yield return null;
            }

            BirdGameManager.Instance.OnGameResult += ShowResult;
            BirdGameManager.Instance.OnGameResultEnded += CloseUI;
        }

        private void OnDisable()
        {
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.OnGameResult -= ShowResult;
                BirdGameManager.Instance.OnGameResultEnded -= CloseUI;
            }
        }

        private void ShowResult(bool isSeekerWin, int survivorCount)
        {
            resultPanel.SetActive(true);

            if (isSeekerWin)
            {
                winTitleText.text = "술래 승리";
                statsText.text = "술래가 모든 생존자를 찾아넀습니다";
            }
            else
            {
                winTitleText.text = "생존자 승리";
                statsText.text = $"살아남은 생존자 : {survivorCount}";
            }

            if (canvasGroup != null)
            {
                StopAllCoroutines();
                StartCoroutine(Co_FadeIn());
            }
        }

        private IEnumerator Co_FadeIn()
        {
            float duration = 0.5f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        private void OnLobbyButtonClicked()
        {
            Debug.Log("[Bird] 로비로 돌아갑니다.");
            
            if (BirdGameManager.Instance != null && BirdGameManager.Instance.Runner != null)
            {
                BirdGameManager.Instance.Runner.Shutdown();
            }
            else if (NetworkRunner.Instances.Count > 0) 
            {
                NetworkRunner.Instances[0].Shutdown();
            }
            /*
            var runner = FindObjectOfType<NetworkRunner>();
            if (runner != null)
            {
                runner.Shutdown();
            }
            */
        }

        private void CloseUI()
        {
            resultPanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }
    }
}
