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
            // 방어 코드: 이미 파괴되었거나 유효하지 않은 경우 무시
            if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;
            
            var target = foreign.GetComponentInParent<BirdPlayerController>();
            
            // 1. 플레이어를 맞춘 경우
            if (target != null)
            {
                // 자신(발사자)은 맞출 수 없음
                if (target.Object.InputAuthority == Owner) return;

                if (launcher != null)
                {
                    launcher.NotifyBulletHit();
                }
                target.TakeDamage(10, Owner);
                Runner.Despawn(Object); // 명중 시 소멸
            }
            // 2. 플레이어가 아닌 환경이나 바닥에 맞은 경우
            else
            {
                // 생성 직후(예: 0.05초 이내) 바닥에 닿는 경우 무시 (발사 위치가 낮을 때 대비)
                if (destroyTimer.RemainingTime(Runner) > lifeTime - 0.005f) return;

                if (launcher != null)
                {
                    launcher.NotifyBulletMiss();
                }
                Runner.Despawn(Object);
            }
        }
        
    }
}
