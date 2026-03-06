using System;
using System.Collections.Generic;
using Bird.Network.Data;
using Bird.Network.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bird.Network.Handlers
{
    /// <summary>
    /// 네트워크 접속 및 세션 관리를 담당하게 될 핸들러입니다.
    /// </summary>
    public class BirdNetworkHandler : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private string gameSceneName = "GameScene"; // 유니티 빌드 설정에 등록된 게임 씬 인덱스나 이름
        private NetworkRunner currentRunner;
        
        private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

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

        /// <summary>
        /// 플레이어가 서버에 접속했을 때 호출됩니다.
        /// Instantiate와 runner.Spawn의 차이
        /// Instantiate : 내 컴퓨터 메모리에만 물체를 만듦. 다른 사람 컴퓨터는 이 사실을 전혀 모릅니다.
        /// runner.Spawn : 서버 메모리에 물체를 등록하고, 연결된 모든 클라이언트에게 똑같은 주민번호(NetworkID)를 가진 물체를 너희 메모리에도 만들어! 라고 명령을 보냅니다.
        /// 결과적으로 모든 유저의 메모리 상에 동일한 ID를 공유하는 객체가 존재하게 되어 동기화가 가능해집니다.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return; // 서버 권한을 가진 사람만 생성 권한이 있습니다.
            if (spawnedCharacters.ContainsKey(player)) return;
            
            Debug.Log($"[Bird] 플레이어 접속 : {player}. 캐릭터를 생성합니다.");
            
            Vector3 spawnPos = new Vector3(Random.Range(-3, 3), 1, Random.Range(-3, 3));
            var playerObject = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            
            spawnedCharacters.Add(player, playerObject);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // 플레이어가 나갔을 때 딕셔너리에서 제거 및 오브젝트 파괴
            if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                spawnedCharacters.Remove(player);
                Debug.Log($"[Bird] 플레이어 퇴장 : {player}. 캐릭터를 제거했습니다.");
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new BirdInputData();
            
            // 키보드 입력
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 keyboardInput = new Vector3(x, 0, z);
            
            // 조이스틱 입력과 키보드 입력 중 값이 있는 것을 선택
            if (BirdInputManager.Movement.magnitude > 0)
            {
                data.Movement = BirdInputManager.Movement.normalized;
            }
            else
            {
                data.Movement = keyboardInput.normalized;
            }
            
            // Fusion 엔진에 입력값 전달
            input.Set(data);
        }
        
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnSceneLoadDone(NetworkRunner runner) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }
    }

}
