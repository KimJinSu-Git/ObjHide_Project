using System;
using System.Collections.Generic;
using Bird.Network.Data;
using Bird.Network.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        [SerializeField] private NetworkObject gameManagerPrefab;
        [SerializeField] private string gameSceneName = "GameScene";
        
        private NetworkRunner currentRunner;
        private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        private EventSystem _eventSystem;
        
        private bool _isConnecting = false;

        public async void StartGame(GameMode mode)
        {
            if (_isConnecting) return;
            _isConnecting = true;
            
            Debug.Log($"[Bird] 맵 데이터 다운로드 및 씬 로드 중... ({gameSceneName})");
            await Addressables.LoadSceneAsync(gameSceneName).Task;
            Debug.Log("[Bird] 씬 로드 완료! 네트워크 접속을 시작합니다.");
            
            _eventSystem = EventSystem.current;
            _eventSystem.enabled = false;
            
            if (currentRunner != null)
            {
                Destroy(currentRunner.gameObject);
                currentRunner = null;
            }
            
            currentRunner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(currentRunner.gameObject);
            
            // 네트워크 인터페이스 활성화
            currentRunner.ProvideInput = true;
            
            var sceneManager = currentRunner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null) sceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            
            // 세션 시작 (방 이름 "BirdRoom"으로 고정 테스트)
            var result = await currentRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "BirdRoom",
                SceneManager = sceneManager
            });

            if (result.Ok)
            {
                Debug.Log($"[Bird] {mode} 성공. 게임 씬으로 이동합니다.");
                
                if (currentRunner.IsServer)
                {
                    currentRunner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
                }
                
                if (_eventSystem != null) _eventSystem.enabled = true;
            }
            else
            {
                Debug.LogError($"[Bird] 접속 실패: {result.ShutdownReason}");
                _isConnecting = false;
                
                if (_eventSystem != null) _eventSystem.enabled = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                BirdInputManager.IsJumpPressed = true;
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
            
            // 핵심 : 퓨전 엔진에 이 플레이어의 대표 몸체가 누구인지 알려줍니다.
            runner.SetPlayerObject(player, playerObject);
            
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

            if (runner.IsServer)
            {
                var gameManager = Managers.BirdGameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.HandlePlayerLeft(player);
                }
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[Bird] 네트워크 세션이 종료되었습니다. 사유: {shutdownReason}");

            _isConnecting = false;
            
            if (currentRunner != null)
            {
                Destroy(currentRunner.gameObject, 0.1f);
                currentRunner = null; 
            }

            spawnedCharacters.Clear();

            SceneManager.LoadScene(0);
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
            
            if (BirdInputManager.IsJumpPressed)
            {
                data.Buttons.Set(PlayerInputButtons.Jump, true);
                BirdInputManager.IsJumpPressed = false;
            }
            
            // 카메라의 수평 회전(Yaw) 값을 서버로 전달
            data.LookYaw = CameraRotationHandler.CurrentYaw;
            
            // Fusion 엔진에 입력값 전달
            input.Set(data);
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            /*if (runner.IsServer)
            {
                runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            }*/
        }
        
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

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

        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
