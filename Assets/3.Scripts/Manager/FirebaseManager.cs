using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Bird.Network.Managers
{
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        private FirebaseAuth _auth;
        private FirebaseUser _user;

        public static event Action<FirebaseUser> OnLoginSuccess;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            Debug.Log("[Firebase] 의존성 체크 시작");
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
                    Debug.Log($"[Firebase] 기존 로그인 정보 발견! 자동 로그인 완료 UID: {_user.UserId}");
            
                    OnLoginSuccess?.Invoke(_user);
                    return; 
                }
                
                Debug.Log("[Firebase] 익명 로그인 시도 중");
                var result = await _auth.SignInAnonymouslyAsync();

                _user = result.User;
                Debug.Log($"[Firebase] 로그인 성공 ! UID : {_user.UserId}");

                OnLoginSuccess?.Invoke(_user);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] 로그인 실패 : {e.Message}");
            }
        }
        
        public string GetUserId() => _user != null ? _user.UserId : string.Empty;
    }
}
