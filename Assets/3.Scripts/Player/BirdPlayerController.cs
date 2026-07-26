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

        private float _defaultGravity = -20f;
        
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
            
            if (_ncc != null)
            {
                _defaultGravity = _ncc.gravity;
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
                    
                    if (!isLockedNow)
                    {
                        if (jumpPressed && _ncc.Grounded)
                        {
                            _ncc.Jump();
                            JumpCount++;
                        }

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
                    else
                    {
                        _ncc.Velocity = Vector3.zero;
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
            
            RPC_SetGravityState(!willLock);

            if (PropAlignmentHandler.Instance != null)
            {
                PropAlignmentHandler.Instance.SetSubButtonsVisible(willLock);
            }
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetGravityState(bool enableGravity)
        {
            if (_ncc != null)
            {
                // 중력을 켜야하면 원래 중력값 복구, 꺼야하면 0으로 설정
                _ncc.gravity = enableGravity ? _defaultGravity : 0f;
                
                // 만약 중력을 끄는 거라면, 현재 떨어지고 있던 가속도(Velocity.y)도 즉시 지워버림
                if (!enableGravity)
                {
                    Vector3 currentVel = _ncc.Velocity;
                    currentVel.y = 0f;
                    _ncc.Velocity = currentVel;
                }
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
    }
}
