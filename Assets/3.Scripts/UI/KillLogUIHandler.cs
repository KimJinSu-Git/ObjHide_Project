using System;
using System.Collections;
using Bird.Network.Managers;
using TMPro;
using UnityEngine;

namespace Bird.Network.UI
{
    public class KillLogUIHandler : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 1.5f;

        private Coroutine _logCoroutine;
        private WaitForSeconds _waitForSeconds;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            _waitForSeconds = new WaitForSeconds(displayDuration);
        }

        private IEnumerator Start()
        {
            while (BirdGameManager.Instance == null)
            {
                yield return null;
            }

            BirdGameManager.Instance.OnPlayerKilled += ShowKillLog;
        }

        private void OnDisable()
        {
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.OnPlayerKilled -= ShowKillLog;
            }
        }
        
        private void ShowKillLog(string attacker, string victim)
        {
            if (_logCoroutine != null) StopCoroutine(_logCoroutine);
            
            logText.text = $"<color=#FF5555>{attacker}</color> caught <color=#55AAFF>{victim}</color>";
            
            _logCoroutine = StartCoroutine(Co_ProcessLog());
        }
        
        private IEnumerator Co_ProcessLog()
        {
            float elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.2f);
                yield return null;
            }
            canvasGroup.alpha = 1;
            
            yield return _waitForSeconds;

            elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / 0.5f);
                yield return null;
            }
            canvasGroup.alpha = 0;
        }
    }
}
