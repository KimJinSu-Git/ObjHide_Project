using System;
using Bird.Network.Data;
using Bird.Network.Managers;
using Bird.Network.UI;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bird.Network.Player
{
    public class BirdPlayerController : NetworkBehaviour
    {
        public static BirdPlayerController Local { get; private set; }
        
        [Header("Prop Settings")]
        [SerializeField] private PropDatabase propDatabase;
        [SerializeField] private Transform meshContainer; // 모델링이 생성될 부모 오브젝트
        [SerializeField] private GameObject defaultVisual; // 기본 모델
        
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 3, -6); // 카메라 위치 오프셋

        [SerializeField] private GameObject bulletPrefab; // 탄환 프리팹
        
        private CharacterController controller;
        private Camera mainCamera;

        // 네트워크 변수 : 이 값이 바뀌면 모든 클라이언트의 Render()가 감지합니다.
        [Networked] public int CurrentPropID { get; set; } = -1; // -1은 기본 상태
        [Networked] public int LastAppliedPropID { get; set; } = -2;
        [Networked] public int CurrentHP { get; set; }
        [Networked] public TickTimer fireCooldown { get; set; }
        [Networked] private int activeBulletsFormLastShot { get; set; } = 0;
        [Networked] private bool didAnyBulletHit { get; set; } = false;
        
        public override void Spawned()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            
            // 내가 조종하는 캐릭터라면 카메라를 내 뒤로 배치 HasInputAuthority(이 캐릭터가 내 조이스틱 입력을 받는 주인공인가?를 묻는 질문입니다.)
            if (HasInputAuthority)
            {
                Local = this;
                Debug.Log("[Bird] 내 캐릭터 카메라 설정 완료");

                if (FireButtonHandler.Instance != null)
                {
                    FireButtonHandler.Instance.SetUpButton(RequestFire);
                }
            }
            
            // 초기 외형 설정
            UpdateAppearance();
        }

        // Fusion 2에서 [Networked] 변수가 변경될 때 시각적 업데이트를 처리하는 함수입니다.
        public override void Render()
        {
            // 이전 프레임과 값이 다를 때만 외형 업데이트
            UpdateAppearance();
        }

        private void LateUpdate()
        {
            if (HasInputAuthority && mainCamera != null)
            {
                // 카메라 회전 적용 (수평 회전만)
                Quaternion rotation = Quaternion.Euler(0, CameraRotationHandler.CurrentYaw, 0);
                
                // 캐릭터 뒤편에 카메라 배치
                Vector3 rotatedOffset = rotation * cameraOffset;
                mainCamera.transform.position = transform.position + rotatedOffset;
                
                // 캐릭터를 쳐다보도록
                mainCamera.transform.LookAt(transform.position + Vector3.up);
            }
        }

        /// <summary>
        /// Fusion 전용 업데이트 (물리 및 동기화 계산용)
        /// FixedUpdateNetwork는 프레임(FPS)과 독립적으로 네트워크 틱마다 실행됩니다. 따라서, Time.deltaTime 대신 반드시 Runner.DeltaTime을 사용해야 네트워크 속도에 맞는 부드러운 이동이 가능합니다.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            // 서버로부터 내 입력 데이터를 가져옴
            if (GetInput(out BirdInputData data))
            {
                // 이동 방향 계산 (카메라 회전 Yaw 값을 적용)
                // 캐릭터가 카메라가 바라보는 방향을 기준으로 움직이게 합니다.
                Quaternion lookRotation = Quaternion.Euler(0, CameraRotationHandler.CurrentYaw, 0);
                Vector3 moveDirection = lookRotation * data.Movement;
        
                Vector3 moveVector = moveDirection * moveSpeed * Runner.DeltaTime;
        
                if (!controller.isGrounded)
                {
                    moveVector.y -= 9.81f * Runner.DeltaTime;
                }

                // 실제 이동 수행
                if (controller != null && controller.enabled)
                {
                    controller.Move(moveVector);
                }
        
                if (moveDirection.magnitude > 0.1f)
                {
                    transform.forward = moveDirection;
                }
            }
            
            UpdatePlayerBehaviourByPhase();
        }

        private void UpdateAppearance()
        {
            if (propDatabase == null || meshContainer == null) return;
            
            // if (LastAppliedPropID == CurrentPropID) return;
            
            // 기존 매쉬 자식들을 모두 제거
            foreach (Transform child in meshContainer) Destroy(child.gameObject);

            // ID가 -1이면 기본 외형 표시
            if (CurrentPropID == -1)
            {
                if (HasStateAuthority && CurrentHP == 0) CurrentHP = 100;
                
                if (defaultVisual != null) defaultVisual.SetActive(true);
                ResetCollider();
            }
            else
            {
                var data = propDatabase.GetPropByID(CurrentPropID);
                if (data != null)
                {
                    // 체력 설정 (서버 권한)
                    if (HasStateAuthority) CurrentHP = data.MaxHP;
                
                    if (defaultVisual != null) defaultVisual.SetActive(false);
                    // 모델 생성
                    var prop = Instantiate(data.PropPrefab, meshContainer);
                    int layer = LayerMask.NameToLayer("PropPlayer");
                    SetLayerRecursive(prop, layer);
                
                    var cc = GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = false;
                        cc.height = data.Height;
                        cc.radius = data.Radius;
                        cc.center = data.Center;
                        cc.enabled = true;
                    }
                }
            }
            // LastAppliedPropID = CurrentPropID;
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private void ResetCollider()
        {
            controller.center = new Vector3(0, 0, 0);
            controller.height = 2f;
            controller.radius = 0.5f;
        }

        private void UpdatePlayerBehaviourByPhase()
        {
            if (BirdGameManager.Instance == null) return;
            if (!BirdGameManager.Instance.Object || !BirdGameManager.Instance.Object.IsValid) return;

            bool isSeeker = Runner.LocalPlayer == BirdGameManager.Instance.Seeker;

            if (HasInputAuthority && FireButtonHandler.Instance != null)
            {
                bool shouldShowButton = isSeeker && BirdGameManager.Instance.CurrentPhase != GamePhase.Lobby;
                FireButtonHandler.Instance.SetVisible(shouldShowButton);
            }

            if (BirdGameManager.Instance.CurrentPhase == GamePhase.Ready)
            {
                if (isSeeker)
                {
                    // 술래는 맵을 미리 정찰할 수 있지만, 도망자들이 보이지 않아야 함
                    ApplySeekerVision(true);
                }
                else
                {
                    ApplySeekerVision(false);
                }
            }
            else if (BirdGameManager.Instance.CurrentPhase == GamePhase.Hide)
            {
                // 술래 시야 복구
                ApplySeekerVision(false);
            }
        }

        private void ApplySeekerVision(bool isReadyPhase)
        {
            if (!HasInputAuthority) return;
            
            int propLayer = LayerMask.NameToLayer("PropPlayer");
            if (isReadyPhase)
            {
                Camera.main.cullingMask &= ~(1 << propLayer);
            }
            else
            {
                Camera.main.cullingMask |= (1 << propLayer);
            }
        }

        public void TakeDamage(int damage, PlayerRef attacker)
        {
            if (!HasStateAuthority) return;
            if (CurrentHP <= 0) return;

            CurrentHP -= damage;
            Debug.Log($"[Bird] {Object.InputAuthority} 피격! 남은 체력은 : {CurrentHP}입니다.");
            
            if (CurrentHP <= 0)
            {
                // 도망자가 죽었을 때 술래의 체력을 회복 시킴
                if (attacker != PlayerRef.None && attacker != Object.InputAuthority)
                {
                    var seekerObj = Runner.GetPlayerObject(attacker);
                    if (seekerObj != null)
                    {
                        var seekerController = seekerObj.GetComponent<BirdPlayerController>();
                        seekerController?.Heal(50);
                    }
                }
                OnDeath();
            }
        }

        public void Heal(int amount)
        {
            if (!HasStateAuthority) return;
            CurrentHP = (int)MathF.Min(CurrentHP + amount, 100);
            Debug.Log($"[Bird] 도망자 사망 => 술래 회복. 현재 체력 : {CurrentHP}");
        }

        private void OnDeath()
        {
            // 사망처리 (TODO :: 관전자 모드 전환 추가 예정)
            Debug.Log($"{Object.InputAuthority} 플레이어 사망!");
            if (BirdGameManager.Instance.Seeker == Object.InputAuthority)
            {
                Debug.Log("술래 사망");
                // BirdGameManager.Instance.EndRound(false); // 술래 패배
            }
        }

        /// <summary>
        /// 총기 발사 버튼 클릭시 호출 될 함수
        /// </summary>
        public void RequestFire()
        {
            if (!HasInputAuthority) return;

            if (!fireCooldown.ExpiredOrNotRunning(Runner) && fireCooldown.IsRunning) return;

            // 쿨타임 1초 설정
            fireCooldown = TickTimer.CreateFromSeconds(Runner, 1f);
            
            // Vector3 fireorigin = mainCamera.transform.position + (mainCamera.transform.forward * 1.5f);
            // Vector3 fireDirection = mainCamera.transform.forward;
            
            RPC_SpawnProjectile(transform.position + transform.forward * 1f, transform.forward);
        }

        // 총알이 명중했을 때 호출
        public void NotifyBulletHit()
        {
            didAnyBulletHit = true;
            activeBulletsFormLastShot--;
            CheckShotResult();
        }

        // 총알이 빗나가서 소멸했을 때 호출
        public void NotifyBulletMiss()
        {
            activeBulletsFormLastShot--;
            CheckShotResult();
        }

        private void CheckShotResult()
        {
            // 발사한 5발이 모두 처리가 끝났을 때
            if (activeBulletsFormLastShot <= 0)
            {
                // 단 한발도 명중하지 못했을 때
                if (!didAnyBulletHit)
                {
                    Debug.Log("[Bird] 한 발도 명중하지 못했으므로 페널티가 부여됩니다");
                    TakeDamage(10, Object.InputAuthority);
                }
                else
                {
                    Debug.Log("[Bird] 적중한 탄환이 있어 페널티를 면제합니다.");
                }
            }
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SpawnProjectile(Vector3 origin, Vector3 direction)
        {
            activeBulletsFormLastShot = 5;
            didAnyBulletHit = false;
            
            for (int i = 0; i < 5; i++)
            {
                Vector3 spread = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                Quaternion rotation = Quaternion.LookRotation(direction + spread);

                var bulletObj = Runner.Spawn(bulletPrefab, origin, rotation, Object.InputAuthority);
                
                var bulletScript = bulletObj.GetComponent<BirdBullet>();
                if (bulletScript != null)
                {
                    bulletScript.Owner = Object.InputAuthority;
                    bulletScript.Setup(this);
                }
            }
        }
        
        /// <summary>
        /// 클라이언트가 서버에게 "나 변신시켜줘"라고 요청하는 함수입니다.
        /// Networked => 모든 사람이 보고 있는 전광판입니다. (서버만 수정할 수 있습니다)
        /// RPC => 손님이 주방에 전달하는 주문서 입니다.
        /// 손님(클라이언트)이 전광판에 올라가서 자기 맘대로 메뉴를 고칠 수는 없습니다. 대신 주문서(RPC)를 주방(서버)에 보내면, 요리사가 확인하고 전광판(CurrentPropID)을 업데이트해주는 방식입니다.
        /// InputAuthority => 이 명령을 보낼 수 있는 사람(이 캐릭터를 실제로 조종하고 있는 클라이언트(내 컴퓨터)가 발신자임을 의미합니다)
        /// StateAuthority => 이 명령을 받아서 실행할 사람. 게임의 모든 중요한 판정은 서버가 담당해야 하므로 서버가 수신자가 됩니다.
        /// </summary>
        /// <param name="propID"></param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_RequestChangeProp(int propID)
        {
            // 서버에서만 이 로직이 실행됩니다.
            // 여기서 검증(준비 시간인지 등)을 거친 후 값을 바꿔줍니다.
            var gameManager = BirdGameManager.Instance;
            if (gameManager.CurrentPhase == GamePhase.Ready || gameManager.CurrentPhase == GamePhase.Reroll || gameManager.CurrentPhase == GamePhase.Lobby) // Lobby는 솔로로 테스트하기 위해 잠시 넣어뒀음. 나중에 지워야함
            {
                CurrentPropID = propID;
                Debug.Log($"[Bird] 서버가 {Object.InputAuthority}의 사물을 {propID}번으로 변경을 승인하였습니다.");
            }
        }
    }
}

/*
 * 개발 중 발생했던 문제점 => 플레이어 이동 동기화 이슈
 * 상황 => 클라이언트의 이동 속도가 호스트보다 2배 가량 빠름 + 호스트와 클라이언트의 위치 불일치 발생
 * 원인 => 이중 위치 연산 및 물리 - 네트워크 간섭
 * 1. 컴포넌트 간의 주도권 싸움 :
 *  - 기존 NetworkTransform은 단순히 오브젝트의 Transform(좌표)을 강제로 맞추려 합니다.
 *  - 반면 CharacterController는 유니티의 물리 엔진을 바탕으로 스스로 이동하려고 합니다.
 *  - 클라이언트 화면에서는 내가 직접 움직이는 힘과 네트워크가 강제로 맞추려는 힘이 동시에 작용하여 가속도가 붙거나 위치가 튀게 된 현상입니다.
 * 
 * 2. 클라이언트 예측의 부재 :
 *  - 일반 NetworkTransform은 CharacterController가 물리적으로 이동한 결과를 즉각적으로 네트워크 틱에 통합하지 못합니다.
 *  - NetworkCharacterController는 내부적으로 물리 이동 -> 네트워크 틱에 기록 -> 클라이언트 예측 반영 과정을 하나로 묶어 처리하므로 이 충돌을 해결합니다.
 * 
 * 해결 => 기존의 CharacterController와 NetworkTransform을 사용하던 방식을 폐기하고, CharacterController와 NetworkCharacterController를 사용하도록 변경함으로써 해결되었습니다.
 * 비유 설명 => 기차(NetworkTransform) 위에 올라탄 사람(CharacterController)이 앞으로 달려가면, 밖에서 볼 때 기차 속도+사람 속도가 합쳐져 보이듯 빨라졌던 것입니다. NetworkCharacterController는 사람을 기차의 일부로 고정시켜 주는 역할을 합니다.
 */
