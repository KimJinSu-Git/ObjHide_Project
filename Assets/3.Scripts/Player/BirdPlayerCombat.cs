using Fusion;
using UnityEngine;
using Bird.Network.Managers;
using Bird.Network.UI;

namespace Bird.Network.Player
{
    public class BirdPlayerCombat : NetworkBehaviour
    {
        [Networked] public TickTimer fireCooldown { get; set; }
        
        [SerializeField] private BirdGunFX _gunFX;
        
        private BirdPlayerHealth _health;
        private BirdPlayerController _controller;

        public override void Spawned()
        {
            _health = GetComponent<BirdPlayerHealth>();
            _controller = GetComponent<BirdPlayerController>();
            
            if (HasInputAuthority && FireButtonHandler.Instance != null)
            {
                FireButtonHandler.Instance.SetUpButton(RequestFire);
            }
        }

        public void RequestFire()
        {
            if (!HasInputAuthority || _health.CurrentHP <= 0) return;
            if (!fireCooldown.ExpiredOrNotRunning(Runner) && fireCooldown.IsRunning) return;

            fireCooldown = TickTimer.CreateFromSeconds(Runner, 1f);
            
            // 카메라 정보를 컨트롤러나 카메라 핸들러로부터 가져옴
            Camera mainCam = Camera.main;
            
            RPC_PlayShootAnimation();
            
            RPC_FireHitscan(mainCam.transform.position, mainCam.transform.forward);
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_PlayShootAnimation()
        {
            _controller.Visual.TriggerShootAnimation();
            
            if (_gunFX != null)
            {
                _gunFX.PlayFireEffects();
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_FireHitscan(Vector3 origin, Vector3 direction)
        {
            bool hitAnything = false;
            int layerMask = LayerMask.GetMask("PropPlayer", "Environment");
            
            for (int i = 0; i < 5; i++)
            {
                Vector3 spread = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                if (Physics.Raycast(origin, (direction + spread).normalized, out RaycastHit hit, 100f, layerMask))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PropPlayer"))
                    {
                        var targetHealth = hit.collider.GetComponentInParent<BirdPlayerHealth>();
                        if (targetHealth != null && targetHealth.Object.InputAuthority != Object.InputAuthority)
                        {
                            hitAnything = true;
                            targetHealth.TakeDamage(10, Object.InputAuthority);
                        }
                    }
                }
            }
            
            if (!hitAnything && BirdGameManager.Instance.CurrentPhase != GamePhase.Fever)
            {
                _health.TakeDamage(5, Object.InputAuthority);
            }
        }
    }
}
