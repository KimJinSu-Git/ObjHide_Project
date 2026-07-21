using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bird.Network.Player
{
    /// <summary>
    /// 로컬 환경에서 시각/청각 효과를 재생합니다
    /// </summary>
    public class BirdGunFX : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private GameObject muzzleFlashObj;
        
        [Header("Audio")]
        [SerializeField] private AudioClip gunShotSFX;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        
        private ParticleSystem[] _muzzleParticles;

        private void Awake()
        {
            if (muzzleFlashObj != null)
            {
                _muzzleParticles = muzzleFlashObj.GetComponentsInChildren<ParticleSystem>();
            }
        }

        public void PlayFireEffects()
        {
            // 총구 화염 파티클 재생
            if (muzzleFlashObj != null)
            {
                foreach (var p in _muzzleParticles)
                {
                    p.Play();
                }
            }

            // 샷건 소리 재생
            if (audioSource != null && gunShotSFX != null)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(gunShotSFX);
            }
        }
    }
}