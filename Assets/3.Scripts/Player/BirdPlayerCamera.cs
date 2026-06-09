using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Bird.Network.Managers;
using Bird.Network.UI;

namespace Bird.Network.Player
{
    public enum CameraMode { FPS, TPS, FreeLook }

    public class BirdPlayerCamera : NetworkBehaviour
    {
        [Header("Camera Settings")] 
        [SerializeField] private Transform fpsCameraAnchor; 
        [SerializeField] private GameObject gunModel; 
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0.8f, -3); 
        
        private CameraMode currentCameraMode;
        private Camera mainCamera;
        private bool? isLocalSeekerCached = null;
        private BirdPlayerController _controller;

        // 전략 저장소
        private Dictionary<CameraMode, ICameraStrategy> _strategies;
        private ICameraStrategy _currentStrategy;
        
        public bool LocalIsLocked { get; private set; }

        [Networked] public NetworkBool IsLocked { get; set; } 

        public override void Spawned()
        {
            _controller = GetComponent<BirdPlayerController>();
            mainCamera = Camera.main;

            // 전략 클래스 초기화
            _strategies = new Dictionary<CameraMode, ICameraStrategy>
            {
                { CameraMode.FPS, new FpsCameraStrategy() },
                { CameraMode.TPS, new TpsCameraStrategy() },
                { CameraMode.FreeLook, new FreeLookCameraStrategy() }
            };
            
            // 초기 전략 설정 (TPS)
            SetStrategy(CameraMode.TPS);
            
            if (HasInputAuthority)
            {
                BirdPlayerHealth.OnLocalDeath += HandlePlayerDeath;
            }
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
            {
                BirdPlayerHealth.OnLocalDeath -= HandlePlayerDeath;
            }
        }

        public override void Render()
        {
            if (HasInputAuthority && BirdGameManager.Instance != null)
            {
                var currentPhase = BirdGameManager.Instance.CurrentPhase;
                if (currentPhase != GamePhase.Lobby && BirdGameManager.Instance.Seeker != PlayerRef.None)
                {
                    bool isSeeker = Runner.LocalPlayer == BirdGameManager.Instance.Seeker;
                    if (isLocalSeekerCached != isSeeker)
                    {
                        isLocalSeekerCached = isSeeker;
                        
                        CameraMode nextMode = isSeeker ? CameraMode.FPS : CameraMode.TPS;
                        SetStrategy(nextMode);
                        
                        if (gunModel != null) gunModel.SetActive(isSeeker);
                        
                        if (GameplayHUD.Instance != null)
                        {
                            GameplayHUD.Instance.SetCrosshairVisible(isSeeker);
                        }
                    }
                }
            }
        }
        
        private void HandlePlayerDeath()
        {
            // 사망 시 시점을 죽은 위치로 초기화하고 자유 시점으로 전환
            CameraRotationHandler.SetInitialFreePos(mainCamera.transform.position);
            SetStrategy(CameraMode.FreeLook);
    
            if (gunModel != null) gunModel.SetActive(false);
        }

        private void SetStrategy(CameraMode mode)
        {
            if (_strategies.TryGetValue(mode, out var strategy))
            {
                currentCameraMode = mode;
                _currentStrategy = strategy;
                _currentStrategy.OnEnter(mainCamera.transform, transform);
            }
        }

        public void UpdateCamera()
        {
            if (!HasInputAuthority || mainCamera == null || _currentStrategy == null) return;

            // 전략 클래스에 넘겨줄 데이터 가방
            CameraUpdateParams p = new CameraUpdateParams
            {
                Pitch = CameraRotationHandler.CurrentPitch,
                Yaw = CameraRotationHandler.CurrentYaw,
                Offset = cameraOffset,
                Anchor = fpsCameraAnchor,
                Input = GetCameraInput()
            };

            _currentStrategy.UpdateCamera(mainCamera.transform, transform, p);
        }

        private Vector3 GetCameraInput()
        {
            Vector3 joyInput = BirdInputManager.Movement;
            if (joyInput.sqrMagnitude < 0.01f)
            {
                joyInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            }
            return joyInput;
        }

        public void ToggleLock()
        {
            if (!HasInputAuthority || _controller.Health.CurrentHP <= 0) return;
            
            LocalIsLocked = !LocalIsLocked;
        
            if (LocalIsLocked) SetStrategy(CameraMode.FreeLook);
            else SetStrategy(CameraMode.TPS);
            
            RPC_SetLocked(LocalIsLocked);
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetLocked(NetworkBool value) => IsLocked = value;

        public void ApplySeekerVision(bool isReadyPhase)
        {
            if (!HasInputAuthority) return;
            int propLayer = LayerMask.NameToLayer("PropPlayer");
            if (isReadyPhase) Camera.main.cullingMask &= ~(1 << propLayer);
            else Camera.main.cullingMask |= (1 << propLayer);
        }
    }
}
