using Bird.Network.Managers;
using Fusion;
using TMPro;
using UnityEngine;

namespace Bird.Network.Player
{
    public class BirdPlayerIdentity : NetworkBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameplateText; // 머리 위 이름표
        [SerializeField] private Transform nameplateCanvas;
        
        private Camera _camera;

        [Networked, OnChangedRender(nameof(OnNicknameChanged))] public NetworkString<_32> Nickname { get; set; }

        private void Start()
        {
            _camera = Camera.main;
        }

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                RPC_SetNickname(AuthFlowManager.LocalNickname);
            }
            
            UpdateNameplate();
        }

        /// <summary>
        /// 클라이언트가 서버에게 내 닉네임을 알리는 RPC 함수
        /// </summary>
        /// <param name="newNickname"></param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetNickname(NetworkString<_32> newNickname)
        {
            Nickname = newNickname;
        }

        private void OnNicknameChanged()
        {
            UpdateNameplate();
        }

        private void UpdateNameplate()
        {
            if (nameplateText != null)
            {
                nameplateText.text = Nickname.ToString();
            }
        }

        private void LateUpdate()
        {
            if (nameplateText != null && _camera != null)
            {
                nameplateCanvas.LookAt(nameplateCanvas.position + _camera.transform.rotation * Vector3.forward, _camera.transform.rotation * Vector3.up);
            }
            
            HandleNameplateVisibility();
        }

        private void HandleNameplateVisibility()
        {
            if (BirdGameManager.Instance == null || nameplateCanvas == null) return;
            
            var currentPhase = BirdGameManager.Instance.CurrentPhase;
            
            if (currentPhase == GamePhase.Lobby || currentPhase == GamePhase.Result)
            {
                nameplateCanvas.gameObject.SetActive(true);
                return;
            }
            
            nameplateCanvas.gameObject.SetActive(false);
        }
    }
}
