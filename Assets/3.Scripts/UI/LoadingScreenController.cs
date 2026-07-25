using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI statusText;
        
        private float _targetProgress;
        private float _lastDisplayedMB = -1f;

        public void Show(string message)
        {
            loadingPanel.SetActive(true);
            progressSlider.value = 0;
            _targetProgress = 0;
            _lastDisplayedMB = -1f;
            statusText.text = message;
        }

        public void SetProgress(float progress, long downloadedBytes, long totalBytes)
        {
            _targetProgress = progress;

            // 다운로드할 파일이 있을 경우에만 텍스트를 업데이트합니다.
            if (totalBytes > 0)
            {
                // 바이트를 메가바이트로 변환
                float downloadedMB = downloadedBytes / (1024f * 1024f);
                float totalMB = totalBytes / (1024f * 1024f);
                
                float roundedCurrentMB = Mathf.Round(downloadedMB * 10f) / 10f;

                if (!Mathf.Approximately(_lastDisplayedMB, roundedCurrentMB))
                {
                    statusText.text = $"리소스 다운로드 중... ({downloadedMB:F1} / {totalMB:F1} MB)";
                    _lastDisplayedMB = roundedCurrentMB;
                }
            }
            else if (progress >= 1f)
            {
                statusText.text = "게임 접속!";
            }
        }

        public void Hide()
        {
            loadingPanel.SetActive(false);
        }

        private void Update()
        {
            if (!loadingPanel.activeSelf) return;
            progressSlider.value = Mathf.Lerp(progressSlider.value, _targetProgress, Time.deltaTime * 5f);
        }
    }
}
