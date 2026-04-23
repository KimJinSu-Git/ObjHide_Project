using System;
using Bird.Network.Managers;
using Fusion;
using UnityEngine;

namespace Bird.Network.Player
{
    public class BirdPlayerHealth : NetworkBehaviour
    {
        public static event Action<int> OnLocalHpChanged; // currentHP
        public static event Action OnLocalDamaged; // 피격 시
        public static event Action OnLocalDeath; // 사망 시

        [Networked, OnChangedRender(nameof(OnHPChanged))] 
        public int CurrentHP { get; set; }

        private int _lastHP = 100;
        private BirdPlayerController _controller;

        public override void Spawned()
        {
            _controller = GetComponent<BirdPlayerController>();
            _lastHP = CurrentHP;
        }

        private void OnHPChanged()
        {
            if (HasInputAuthority)
            {
                OnLocalHpChanged?.Invoke(CurrentHP);
                if (CurrentHP < _lastHP) OnLocalDamaged?.Invoke();
            }
            _lastHP = CurrentHP;
        }

        public void TakeDamage(int damage, PlayerRef attacker)
        {
            if (!HasStateAuthority || CurrentHP <= 0) return;

            CurrentHP -= damage;
            Debug.Log($"[Bird] {Object.InputAuthority} 피격! 남은 체력: {CurrentHP}");

            if (CurrentHP <= 0)
            {
                BirdGameManager.Instance.NotifyPlayerKilled(attacker, Object.InputAuthority);
                
                if (attacker != PlayerRef.None && attacker != Object.InputAuthority)
                {
                    var seekerObj = Runner.GetPlayerObject(attacker);
                    seekerObj?.GetComponent<BirdPlayerHealth>()?.Heal(50);
                }
                
                OnDeath();
            }
        }

        private void Heal(int amount)
        {
            if (!HasStateAuthority) return;
            CurrentHP = Math.Min(CurrentHP + amount, 100);
        }

        private void OnDeath()
        {
            Debug.Log($"{Object.InputAuthority} 플레이어 사망!");
            
            // Visual에 사망 알림 (외형 숨기기 등)
            GetComponent<BirdPlayerVisual>()?.HandleDeath();

            if (HasInputAuthority)
            {
                OnLocalDeath?.Invoke();
            }
            
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }
    }
}
