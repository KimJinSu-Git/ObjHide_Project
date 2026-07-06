using System;
using System.Threading.Tasks;
using Google;
using UnityEngine;

namespace Bird.Network.Managers
{
    public class GoogleAuthManager : Singleton<GoogleAuthManager>
    {
        [Header("Google Settings")] 
        [SerializeField] private string webClientId = "";

        protected override void Awake()
        {
            base.Awake();

            if (Instance == this)
            {
                InitializeGoogleSignIn();
            }
        }

        /// <summary>
        /// 구글 플러그인을 웹 클라이언트 ID로 초기화합니다.
        /// </summary>
        private void InitializeGoogleSignIn()
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                RequestEmail = true,
                WebClientId = webClientId,
            };
            Debug.Log("[GoogleAuthManager] 구글 로그인 초기화 완료");
        }

        /// <summary>
        /// 구글 로그인 UI를 띄우고 성공 시 ID 토큰을 반환합니다.
        /// </summary>
        public async Task<string> GetGoogleIdTokenAsync()
        {
            try
            {
                Debug.Log("[GoogleAuthManager] 구글 서버에 로그인(토큰 발급) 요청 중 ");

                GoogleSignInUser user = await GoogleSignIn.DefaultInstance.SignIn();
                
                Debug.Log($"[GoogleAuthManager] 구글 로그인 성공 ! 환영합니다 : {user.DisplayName}");
                
                return user.IdToken;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GoogleAuthManager] 구글 로그인 취소 or 실패 : {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 구글 계정에서 로그아웃 합니다.
        /// </summary>
        public void SignOut() => GoogleSignIn.DefaultInstance.SignOut();
    }
}
