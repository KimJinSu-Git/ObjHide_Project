using System.Collections;
using Bird.Network.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class DamageUIHandler : MonoBehaviour
    {
        [SerializeField] private Image damageFlashImage;
        [SerializeField] private float flashDuration = 0.3f;

        private Coroutine flashCoroutine;

        private void Awake()
        {
            if (damageFlashImage != null)
            {
                Color c = damageFlashImage.color;
                c.a = 0f;
                damageFlashImage.color = c;
            }
        }
        
        private void OnEnable()
        {
            BirdPlayerHealth.OnLocalDamaged += ShowDamageFlash;
        }

        private void OnDisable()
        {
            BirdPlayerHealth.OnLocalDamaged -= ShowDamageFlash;
        }

        private void ShowDamageFlash()
        {
            if (damageFlashImage == null) return;
            
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(Co_FlashRed());
        }

        private IEnumerator Co_FlashRed()
        {
            Color c = damageFlashImage.color;
            c.a = 0.5f; 
            damageFlashImage.color = c;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0.5f, 0f, elapsed / flashDuration);
                damageFlashImage.color = c;
                yield return null;
            }

            c.a = 0f;
            damageFlashImage.color = c;
        }
    }
}
