using Bird.Network.Data;
using Bird.Network.Managers;
using Fusion;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Bird.Network.Player
{
    public class BirdPlayerVisual : NetworkBehaviour
    {
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Shoot = Animator.StringToHash("Shoot");

        [Header("Prop Settings")]
        [SerializeField] private PropDatabase propDatabase;
        [SerializeField] private Transform meshContainer;
        [SerializeField] private GameObject defaultVisual;
        [SerializeField] private Vector3 defaultCenter = new Vector3(0, 0.85f, 0);
        
        [Header("IK Settings")]
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform[] spineBones;
        [SerializeField] private float pitchOffset = 0f;

        [Networked, OnChangedRender(nameof(OnPropIDChanged))] 
        public int CurrentPropID { get; set; } = -1;

        private int lastAppliedPropID = -2;
        private BirdPlayerHealth _health;
        private CharacterController _controller;
        
        private AsyncOperationHandle<GameObject> _currentPropHandle;
        
        public Animator PlayerAnimator { get;  private set; }

        public override void Spawned()
        {
            _health = GetComponent<BirdPlayerHealth>();
            _controller = GetComponent<CharacterController>();
            
            if(defaultVisual != null) PlayerAnimator = defaultVisual.GetComponent<Animator>();
            
            UpdateAppearance();
        }
        
        private void LateUpdate()
        {
            float currentPitch = GetComponent<BirdPlayerController>().NetPitch + pitchOffset;

            if (spineBones != null && spineBones.Length > 0)
            {
                float dividedPitch = currentPitch / spineBones.Length;

                foreach (Transform bone in spineBones)
                {
                    if (bone != null)
                    {
                        bone.localRotation = bone.localRotation * Quaternion.Euler(dividedPitch, 0, 0); 
                    }
                }
            }

            if (headBone != null)
            {
                // headBone.localRotation = headBone.localRotation * Quaternion.Euler(-10f, 0, 0); 
            }
        }

        private void OnPropIDChanged() => UpdateAppearance();

        public async void UpdateAppearance()
        {
            if (propDatabase == null || meshContainer == null) return;
            if (lastAppliedPropID == CurrentPropID) return;
            
            if (_currentPropHandle.IsValid())
            {
                Addressables.ReleaseInstance(_currentPropHandle);
            }

            foreach (Transform child in meshContainer) Destroy(child.gameObject);

            if (CurrentPropID == -1)
            {
                if (HasStateAuthority && _health != null && _health.CurrentHP == 0) _health.CurrentHP = 100;
                if (defaultVisual != null) defaultVisual.SetActive(true);
                ResetCollider();
            }
            else
            {
                var data = propDatabase.GetPropByID(CurrentPropID);
                if (data != null)
                {
                    if (HasStateAuthority && _health != null) _health.CurrentHP = data.MaxHP;
                    if (defaultVisual != null) defaultVisual.SetActive(false);
                    
                    _currentPropHandle = data.PropPrefabRef.InstantiateAsync(meshContainer);
                    
                    GameObject prop = await _currentPropHandle.Task;
                    
                    if (prop != null)
                    {
                        int layer = LayerMask.NameToLayer("PropPlayer");
                        SetLayerRecursive(prop, layer);

                        if (_controller != null)
                        {
                            _controller.enabled = false;
                            
                            _controller.height = data.Height;
                            _controller.radius = data.Radius;
                            _controller.center = data.Center;
                            
                            _controller.skinWidth = Mathf.Clamp(data.Radius * 0.1f, 0.001f, 0.08f);
                            
                            _controller.enabled = true;
                        }
                    }
                }
            }
            lastAppliedPropID = CurrentPropID;
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_currentPropHandle.IsValid())
            {
                Addressables.ReleaseInstance(_currentPropHandle);
            }
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private void ResetCollider()
        {
            if (_controller == null) return;
            
            _controller.enabled = false;
            
            _controller.center = defaultCenter;
            _controller.height = 1.5f;
            _controller.radius = 0.3f;
            
            _controller.skinWidth = 0.08f;
            
            _controller.enabled = true;
        }

        public void HandleDeath()
        {
            if (HasStateAuthority) CurrentPropID = -1;
            if (meshContainer != null) meshContainer.gameObject.SetActive(false);
            if (defaultVisual != null) defaultVisual.SetActive(false);
        }

        public void UpdateAnimatorParams(float horizontal, float vertical, bool isGrounded)
        {
            if (CurrentPropID != -1 || PlayerAnimator == null) return;

            PlayerAnimator.SetFloat(Horizontal, horizontal);
            PlayerAnimator.SetFloat(Vertical, vertical);
            PlayerAnimator.SetBool(IsGrounded, isGrounded);
        }
        
        public void TriggerJumpAnimation()
        {
            if (CurrentPropID != -1 || PlayerAnimator == null) return;
            PlayerAnimator.SetTrigger(Jump);
        }

        public void TriggerShootAnimation()
        {
            if (CurrentPropID != -1 || PlayerAnimator == null) return;
            PlayerAnimator.SetTrigger(Shoot);
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
            var gameManager = BirdGameManager.Instance;
            if (gameManager.CurrentPhase == GamePhase.Ready || gameManager.CurrentPhase == GamePhase.Reroll || gameManager.CurrentPhase == GamePhase.Lobby)
            {
                CurrentPropID = propID;
            }
        }
    }
}
