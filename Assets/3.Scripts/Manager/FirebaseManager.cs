using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Bird.Network.Managers
{
    /// <summary>
    /// 의존성 체크 및 익명 로그인을 수행하며 유저 UID를 확보합니다
    /// </summary>
    public class FirebaseManager : Singleton<FirebaseManager>
    {
        private FirebaseAuth _auth;
        private FirebaseUser _user;

        public static event Action<FirebaseUser> OnLoginSuccess;

        private async void Start()
        {
            // Debug.Log("[Firebase] 의존성 체크 시작");
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();

                await SignInAnonymously();
            }
            else
            {
                Debug.LogError($"[Firebase] 의존성 오류 : {dependencyStatus}");
            }
        }

        private void InitializeFirebase()
        {
            _auth = FirebaseAuth.DefaultInstance;
        }


        private async Task SignInAnonymously()
        {
            try
            {
                if (_auth.CurrentUser != null)
                {
                    _user = _auth.CurrentUser;
                    // Debug.Log($"[Firebase] 기존 로그인 정보 발견! 자동 로그인 완료 UID: {_user.UserId}");
            
                    OnLoginSuccess?.Invoke(_user);
                    return; 
                }
                
                // Debug.Log("[Firebase] 익명 로그인 시도 중");
                var result = await _auth.SignInAnonymouslyAsync();

                _user = result.User;
                // Debug.Log($"[Firebase] 로그인 성공 ! UID : {_user.UserId}");

                OnLoginSuccess?.Invoke(_user);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] 로그인 실패 : {e.Message}");
            }
        }

        /// <summary>
        /// 현재 로그인된 익명 계정을 구글 계정과 병합합니다.
        /// </summary>
        public async Task<bool> LinkWithGoogleAsync()
        {
            try
            {
                string idToken = await GoogleAuthManager.Instance.GetGoogleIdTokenAsync();

                if (string.IsNullOrEmpty(idToken))
                {
                    Debug.LogWarning("[Firebase] 구글 로그인 창을 닫았거나 토큰 발급에 실패하였습니다.");
                    return false;
                }

                // 구글 여권(ID Token)을 Firebase가 읽을 수 있는 공식 인증서(Credential)로 변환
                // credential : 각 회사(애플, 구글, 페이스북 등)마다 발급하는 토큰의 생김새가 다른걸 Credential이 Firebase 전용 규격 봉투에 담아 서버가 쉽게 읽을 수 있도록 변환해주는 역할을 담당.
                Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

                FirebaseUser currentUser = _auth.CurrentUser;
                if (currentUser == null)
                {
                    Debug.LogError("[Firebase] 현재 로그인된 유저가 없어 병합할 수 없습니다.");
                    return false;
                }

                // Debug.Log("[Firebase] 구글 계정 병합을 서버에 요청합니다.");

                // 익명 계정에 구글 인증서(Credential)를 붙여서 병합
                // LinkWithCredentialAsync 는 원격 서버(Firebase Auth)에 직접 RPC를 날려 DB 구조를 업데이트하는 무거운 작업이기 때문에 await으로 비동기 처리를 해줘야 유니티 엔진이 멈추지 않습니다.
                AuthResult result = await currentUser.LinkWithCredentialAsync(credential);

                // Debug.Log($"[Firebase] 계정 병합 성공! 이제 데이터는 영구 보존됩니다. 연결된 이메일: {result.User.Email}");
                return true;
            }
            catch (Exception e)
            {
                // 이미 다른 계정에 연동된 구글 아이디를 사용하려 하거나, 통신이 끊긴 경우의 방어적 예외 처리
                Debug.LogError($"[Firebase] 계정 병합 중 에러 발생: {e.Message}");
                return false;
            }
        }
        
        public string GetUserId() => _user != null ? _user.UserId : string.Empty;
    }
}
