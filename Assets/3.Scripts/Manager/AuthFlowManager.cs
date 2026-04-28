using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine;

namespace Bird.Network.Managers
{
    public class AuthFlowManager : MonoBehaviour
    {
        public static string LocalNickname { get; private set; }
        
        private void OnEnable()
        {
            FirebaseManager.OnLoginSuccess += HandleLoginSuccess;
        }

        private void OnDisable()
        {
            FirebaseManager.OnLoginSuccess -= HandleLoginSuccess;
        }

        private async void HandleLoginSuccess(FirebaseUser user)
        {
            await Task.Delay(100);
            
            Debug.Log("[AuthFlow] 로그인 완료.");

            string myNickname = await FirebaseDatabaseManager.Instance.LoadNicknameAsync(user.UserId);

            if (string.IsNullOrEmpty(myNickname) || myNickname == "Unknown" || myNickname == "ErrorName")
            {
                myNickname = GenerateRandomGuestname();
                Debug.Log($"[AuthFlow] 신규 유저입니다. 닉네임 자동 생성: {myNickname}");
                
                await FirebaseDatabaseManager.Instance.SaveNicknameAsync(user.UserId, myNickname);
            }
            else
            {
                Debug.Log($"[AuthFlow] 기존 유저입니다. 환영합니다: {myNickname}");
            }
            
            LocalNickname = myNickname;
            
            await TransitionToLobby(myNickname);
        }

        /// <summary>
        /// 무작위 게스트 닉네임 생성
        /// </summary>
        private string GenerateRandomGuestname()
        {
            int randomNum = Random.Range(1000, 10000);
            return $"Guest_{randomNum}";
        }

        private async Task TransitionToLobby(string nickname)
        {
            Debug.Log($"[AuthFlow] 로비 진입 준비 완료! 확정된 닉네임: {nickname}");
            
            // TODO: 로딩 바 채우기
            
            await Task.Delay(500);

            // TODO: 어드레서블
            
            Debug.Log("[AuthFlow] 로비 씬 전환 완료! (현재는 텍스트만 출력)");
        }
    }
}
