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
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 3, -6); 
        
        private CameraMode currentCameraMode;
        private Camera mainCamera;
        private bool? isLocalSeekerCached = null;
        private BirdPlayerController _controller;

        [Networked] public NetworkBool IsLocked { get; set; } 

        public override void Spawned()
        {
            _controller = GetComponent<BirdPlayerController>();
            mainCamera = Camera.main;
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
                        currentCameraMode = isSeeker ? CameraMode.FPS : CameraMode.TPS;
                        if (gunModel != null) gunModel.SetActive(isSeeker);
                    }
                }
            }
        }

        public void UpdateCamera()
        {
            if (!HasInputAuthority || mainCamera == null) return;

            Quaternion rotation = Quaternion.Euler(CameraRotationHandler.CurrentPitch, CameraRotationHandler.CurrentYaw, 0);
            
            switch (currentCameraMode)
            {
                case CameraMode.FPS:
                    mainCamera.transform.position = fpsCameraAnchor.position;
                    mainCamera.transform.rotation = rotation;
                    break;
                case CameraMode.TPS:
                    Vector3 rotatedOffset = rotation * cameraOffset;
                    mainCamera.transform.position = transform.position + rotatedOffset;
                    mainCamera.transform.rotation = rotation;
                    break;
                case CameraMode.FreeLook:
                    Vector3 joyInput = BirdInputManager.Movement;
                    if (joyInput.sqrMagnitude < 0.01f)
                    {
                        joyInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
                    }
                    mainCamera.transform.position = CameraRotationHandler.GetFreeLookUpdate(mainCamera.transform, joyInput);
                    mainCamera.transform.rotation = rotation;
                    break;
            }
        }

        public void SetFreeLook(Vector3 position)
        {
            CameraRotationHandler.SetInitialFreePos(position);
            currentCameraMode = CameraMode.FreeLook;
        }

        public void ToggleLock()
        {
            if (!HasInputAuthority || _controller.Health.CurrentHP <= 0) return;
        
            bool nextLockState = !IsLocked;
            if (nextLockState)
            {
                SetFreeLook(mainCamera.transform.position);
            }
            else
            {
                currentCameraMode = CameraMode.TPS;
            }
            RPC_SetLocked(nextLockState);
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
