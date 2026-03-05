using Fusion;
using UnityEngine;

namespace Bird.Network.Handlers
{
    /// <summary>
    /// 네트워크 접속 및 세션 관리를 담당하게 될 핸들러입니다.
    /// </summary>
    public class BirdNetworkHandler : MonoBehaviour
    {
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private string gameSceneName = "GameScene"; // 유니티 빌드 설정에 등록된 게임 씬 인덱스나 이름
        private NetworkRunner currentRunner;

        public async void StartGame(GameMode mode)
        {
            if (currentRunner == null)
            {
                currentRunner = Instantiate(runnerPrefab);
                DontDestroyOnLoad(currentRunner.gameObject);
            }

            // 네트워크 인터페이스 활성화
            currentRunner.ProvideInput = true;

            // 세션 시작 (방 이름 "BirdRoom"으로 고정 테스트)
            var result = await currentRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "BirdRoom",
                Scene = SceneRef.FromIndex(1), // Build Settings의 1번 씬이 GameScene일 때
                SceneManager = currentRunner.GetComponent<NetworkSceneManagerDefault>() // NetworkSceneManagerDefault는 씬 전환 시 동기화르 도와주는 친구입니다.
            });

            if (result.Ok)
            {
                Debug.Log($"[Bird] {mode} 성공. 게임 씬으로 이동합니다.");
            }
        }
    }

}
