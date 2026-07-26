using System.Linq;
using Bird.Network.Managers;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class AccountUIHandler : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button linkGoogleButton;

        private void Start()
        {
            linkGoogleButton.onClick.AddListener(OnLinkGoogleButtonClicked);
            
            CheckAccountStatus();
        }

        /// <summary>
        /// 현재 유저가 이미 구글에 연동되어 있는지 확인합니다.
        /// Firebase는 하나의 UID에 여러 개의 로그인 수단(이메일, 구글, 페이스북)을 주렁주렁 매달 수 있는 구조를 가집니다.
        /// user.ProviderData는 여권에 찍힌 비자 스탬프 목록과 같습니다.
        /// LINQ의 Any() 함수를 사용하여 이 유저의 여권에 google.com 이라는 구글 스탬프가 하나라도 있는가?를 빠르게 스캔하는 역할을 합니다.
        /// </summary>
        private void CheckAccountStatus()
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) return;
            
            // ProviderData 배열을 뒤져서 "google.com" 이라는 제공자가 있는지 확인합니다.
            bool isLinkedToGoogle = user.ProviderData.Any(provider => provider.ProviderId == "google.com");

            if (isLinkedToGoogle)
            {
                // 이미 연동된 유저라면 버튼 비활성화 및 텍스트 변경
                linkGoogleButton.enabled = false;
                linkGoogleButton.interactable = false;
                // Debug.Log("[AccountUI] 이 계정은 이미 구글 계정과 안전하게 연동되어 있습니다.");
            }
            else
            {
                linkGoogleButton.enabled = true;
                linkGoogleButton.interactable = true;
            }
        }

        /// <summary>
        /// 연동 버튼을 눌렀을 때 실행되는 비동기 함수입니다.
        /// </summary>
        private async void OnLinkGoogleButtonClicked()
        {
            // 중복 클릭 방지
            linkGoogleButton.interactable = false;

            bool success = await FirebaseManager.Instance.LinkWithGoogleAsync();

            if (success)
            {
                // Debug.Log("[AccountUI] 구글 계정 연동에 성공하여 UI를 갱신합니다.");
                linkGoogleButton.enabled = false;
                linkGoogleButton.interactable = false;
            }
            else
            {
                // Debug.LogWarning("[AccountUI] 구글 계정 연동 실패.");
                linkGoogleButton.enabled = true;
                linkGoogleButton.interactable = true;
            }
        }
    }
}
