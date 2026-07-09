using System;
using Bird.Network.Data;
using Bird.Network.Managers;
using Bird.Network.UI;
using Fusion;
using UnityEngine;

namespace Bird.Network.Player
{
    [RequireComponent(typeof(NetworkCharacterController))]
    [RequireComponent(typeof(BirdPlayerHealth))]
    [RequireComponent(typeof(BirdPlayerVisual))]
    [RequireComponent(typeof(BirdPlayerCombat))]
    [RequireComponent(typeof(BirdPlayerCamera))]
    [RequireComponent(typeof(BirdPlayerIdentity))]
    public class BirdPlayerController : NetworkBehaviour
    {
        public static BirdPlayerController Instance { get; private set; }

        public static event Action<int> OnLocalSpawned;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 5f;
        
        [Networked] private float NetHorizontal { get; set; }
        [Networked] private float NetVertical { get; set; }
        [Networked] private NetworkBool NetIsGrounded { get; set; }
        [Networked] private int JumpCount { get; set; }
        [Networked] private float PropYaw { get; set; }
        [Networked] public float NetPitch { get; set; }
        [Networked] private NetworkButtons PrevButtons { get; set; }

        private float _localPropYaw;
        private int _lastJumpCount = 0;
        
        public BirdPlayerHealth Health { get; private set; }
        public BirdPlayerVisual Visual { get; private set; }
        public BirdPlayerCombat Combat { get; private set; }
        public BirdPlayerCamera CameraHandler { get; private set; }
        public BirdPlayerIdentity Identity { get; private set; }

        private NetworkCharacterController _ncc;

        public override void Spawned()
        {
            _ncc = GetComponent<NetworkCharacterController>();
            Health = GetComponent<BirdPlayerHealth>();
            Visual = GetComponent<BirdPlayerVisual>();
            Combat = GetComponent<BirdPlayerCombat>();
            CameraHandler = GetComponent<BirdPlayerCamera>();
            Identity = GetComponent<BirdPlayerIdentity>();
            
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.RegisterPlayer(Object.InputAuthority, this);
            }
            
            if (HasInputAuthority)
            {
                Instance = this;
                OnLocalSpawned?.Invoke(Health.CurrentHP);
                
                if (PropAlignmentHandler.Instance != null)
                {
                    PropAlignmentHandler.Instance.SetUpButtons(ToggleLockAndSnap, Rotate45, Rotate90);
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.UnregisterPlayer(Object.InputAuthority);
            }
        }
        
        private void Update()
        {
            if (HasInputAuthority && Input.GetKeyDown(KeyCode.L))
            {
                CameraHandler.ToggleLock();
            }
        }

        private void LateUpdate()
        {
            CameraHandler.UpdateCamera();
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out BirdInputData data))
            {
                if (Health.CurrentHP > 0)
                {
                    bool isLockedNow = HasInputAuthority ? CameraHandler.LocalIsLocked : CameraHandler.IsLocked;
                    bool jumpPressed = data.Buttons.WasPressed(PrevButtons, PlayerInputButtons.Jump);
                    
                    Vector3 moveVector = Vector3.zero;
                    
                    if (!isLockedNow)
                    {
                        Quaternion lookRotation = Quaternion.Euler(0, data.LookYaw, 0);
                        Vector3 moveDirection = lookRotation * data.Movement;
                        moveVector = moveDirection * moveSpeed;
                
                        transform.rotation = lookRotation;
                
                        NetHorizontal = data.Movement.x;
                        NetVertical = data.Movement.z;
                        NetPitch = data.LookPitch;
                    }
                    else
                    {
                        float targetYaw = HasInputAuthority ? _localPropYaw : PropYaw;
                        transform.rotation = Quaternion.Euler(0, targetYaw, 0);
                        
                        NetHorizontal = 0;
                        NetVertical = 0;
                    }
                    
                    if (!isLockedNow && jumpPressed && _ncc.Grounded)
                    {
                        _ncc.Jump();
                        JumpCount++;
                    }
                    
                    if (_ncc != null) 
                    {
                        _ncc.Move(moveVector);
                        
                        if (!jumpPressed && _ncc.Grounded)
                        {
                            Vector3 currentVel = _ncc.Velocity;
                            if (currentVel.y > 0)
                            {
                                currentVel.y = 0f; 
                                _ncc.Velocity = currentVel;
                            }
                        }
                    }
                    
                    NetIsGrounded = _ncc.Grounded;
                    PrevButtons = data.Buttons;
                }
            }
        }

        public override void Render()
        {
            Visual.UpdateAnimatorParams(NetHorizontal, NetVertical, NetIsGrounded);
            
            if (_lastJumpCount != JumpCount)
            {
                Visual.TriggerJumpAnimation();
                _lastJumpCount = JumpCount;
            }
            
            UpdatePlayerBehaviourByPhase();
        }
        
        private void ToggleLockAndSnap()
        {
            if (!HasInputAuthority || Health.CurrentHP <= 0) return;

            // 토글 후 잠금 상태가 될 것인지 미리 확인
            bool willLock = !CameraHandler.LocalIsLocked;
            
            CameraHandler.ToggleLock();

            if (willLock)
            {
                float currentYaw = transform.eulerAngles.y;
                _localPropYaw = Mathf.Round(currentYaw / 90f) * 90f; // 90도 스냅 연산
                RPC_SetPropYaw(_localPropYaw);
            }

            if (PropAlignmentHandler.Instance != null)
            {
                PropAlignmentHandler.Instance.SetSubButtonsVisible(willLock);
            }
        }
        
        private void Rotate45()
        {
            _localPropYaw += 45f;
            RPC_SetPropYaw(_localPropYaw);
        }

        private void Rotate90()
        {
            _localPropYaw += 90f;
            RPC_SetPropYaw(_localPropYaw);
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetPropYaw(float yaw)
        {
            PropYaw = yaw;
        }

        private void UpdatePlayerBehaviourByPhase()
        {
            if (BirdGameManager.Instance == null) return;
            bool isSeeker = Runner.LocalPlayer == BirdGameManager.Instance.Seeker;
            var currentPhase = BirdGameManager.Instance.CurrentPhase;

            if (HasInputAuthority)
            {
                bool canShoot = isSeeker && (currentPhase != GamePhase.Lobby && currentPhase != GamePhase.Ready);
                bool canAlign = !isSeeker && currentPhase != GamePhase.Lobby;
                
                if (FireButtonHandler.Instance != null)
                {
                    FireButtonHandler.Instance.SetVisible(canShoot);
                }

                if (PropAlignmentHandler.Instance != null)
                {
                    PropAlignmentHandler.Instance.SetVisible(canAlign);
                }
            }

            if (currentPhase == GamePhase.Ready)
            {
                CameraHandler.ApplySeekerVision(isSeeker);
            }
            else if (currentPhase == GamePhase.Hide)
            {
                CameraHandler.ApplySeekerVision(false);
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
    }
}
