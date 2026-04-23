using System;
using System.Collections;
using Bird.Network.Managers;
using TMPro;
using UnityEngine;

namespace Bird.Network.UI
{
    public class KillLogUIHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private float displayDuration = 1.5f;

        private Coroutine _logCoroutine;
        
        private void Awake() => logText.gameObject.SetActive(false);

        private void OnEnable()
        {
            BirdGameManager.OnPlayerKilled += ShowKillLog;
        }

        private void OnDisable()
        {
            BirdGameManager.OnPlayerKilled -= ShowKillLog;
        }
        
        private void ShowKillLog(string attacker, string victim)
        {
            if (_logCoroutine != null) StopCoroutine(_logCoroutine);
            _logCoroutine = StartCoroutine(Co_ProcessLog(attacker, victim));
        }
        
        private IEnumerator Co_ProcessLog(string attacker, string victim)
        {
            logText.gameObject.SetActive(true);
            logText.text = $"<color=red>{attacker}</color> caught <color=yellow>{victim}</color>!";

            yield return new WaitForSeconds(displayDuration);

            logText.gameObject.SetActive(false);
        }
    }
}
