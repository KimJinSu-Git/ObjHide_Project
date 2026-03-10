using Fusion;
using UnityEngine;

namespace Bird.Network.Player
{
    public class BirdBullet : NetworkBehaviour
    {
        [SerializeField] private float speed = 30f;
        [SerializeField] private float lifeTime = 0.5f;

        private BirdPlayerController launcher;
        private bool isBonusBullet = false; // 여러 발 중 한 발이라도 맞았는지 체크값
        
        // 누구의 총알인지 저장해둘 변수
        [Networked] public PlayerRef Owner { get; set; }
        
        // 서버에서만 작동하는 타이머
        [Networked] public TickTimer destroyTimer { get; set; }

        public void Setup(BirdPlayerController launcher)
        {
            this.launcher = launcher;
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                destroyTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 앞으로 이동
            transform.Translate(Vector3.forward * speed * Runner.DeltaTime);
            
            // 시간이 다 되면 소멸
            if (Object.HasStateAuthority && destroyTimer.Expired(Runner))
            {
                // 아무도 못 맞추고 시간이 다 되어 사라질 때 패널티 체크
                if (launcher != null)
                {
                    // 총알이 사라질 때 못맞췄다고 알림
                    launcher.NotifyBulletMiss();
                }
                Runner.Despawn(Object);
            }
        }

        private void OnTriggerEnter(Collider foreign)
        {
            if (!Object.HasStateAuthority) return;
            
            var target = foreign.GetComponent<BirdPlayerController>();
            
            // 플레이어를 맞춘 경우
            if (target != null && target.Object.InputAuthority != Owner)
            {
                if (launcher != null)
                {
                    launcher.NotifyBulletHit();
                }
                target.TakeDamage(10, Owner);
                Runner.Despawn(Object); // 명중 시 소멸
            }
            else if (foreign.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                if (launcher != null)
                {
                    launcher.NotifyBulletHit();
                }
                Runner.Despawn(Object);
            }
        }
        
    }
}
