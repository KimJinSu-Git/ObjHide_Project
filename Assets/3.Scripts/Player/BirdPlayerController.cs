using System;
using Bird.Network.Data;
using Bird.Network.Managers;
using Bird.Network.UI;
using Fusion;
using UnityEngine;

namespace Bird.Network.Player
{
    public class BirdPlayerController : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 3, -6); // 카메라 위치 오프셋
        
        private CharacterController controller;
        private Camera mainCamera;

        public override void Spawned()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            
            // 내가 조종하는 캐릭터라면 카메라를 내 뒤로 배치 HasInputAuthority(이 캐릭터가 내 조이스틱 입력을 받는 주인공인가?를 묻는 질문입니다.)
            if (HasInputAuthority)
            {
                Debug.Log("[Bird] 내 캐릭터 카메라 설정 완료");
            }
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
                controller.Move(moveVector);
        
                if (moveDirection.magnitude > 0.1f)
                {
                    transform.forward = moveDirection;
                }
            }
            
            UpdatePlayerBehaviourByPhase();
        }

        private void UpdatePlayerBehaviourByPhase()
        {
            if (BirdGameManager.Instance == null) return;
            if (!BirdGameManager.Instance.Object || !BirdGameManager.Instance.Object.IsValid) return;

            bool isSeeker = Runner.LocalPlayer == BirdGameManager.Instance.Seeker;

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
                Camera.main.cullingMask &= ~(1 << propLayer); // TODO ::: 나중에 플레이어들에게 propLayer를 추가할 예정입니다.
            }
            else
            {
                Camera.main.cullingMask |= (1 << propLayer);
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
