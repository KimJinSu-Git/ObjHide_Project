using Firebase.Auth;
using System.Threading.Tasks;
using Bird.Network.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Bird.Network.Managers
{
    public class AuthFlowManager : MonoBehaviour
    {
        [SerializeField] private LoadingScreenController loadingUI;
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
            
            if (loadingUI != null)
            {
                loadingUI.Show("Game Resources Check....");

                // Preload 라벨이 붙은 모든 에셋 다운로드 시도
                await AddressableResourceManager.Instance.PreloadByLabel("Preload", (progress, downloaded, total) => {
                    loadingUI.SetProgress(progress, downloaded, total);
                });

                loadingUI.SetProgress(1.0f, 0, 0);
                await Task.Delay(500);
                loadingUI.Hide();
            }
            
            Debug.Log("[AuthFlow] 모든 리소스 준비 완료! 로비 씬으로 진입합니다.");
            
            await Addressables.LoadSceneAsync("LobbyScene").Task;
        }
    }
}
