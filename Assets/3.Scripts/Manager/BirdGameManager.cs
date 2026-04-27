using System;
using System.Collections.Generic;
using System.Linq;
using Bird.Network.Player;
using Bird.Network.UI;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bird.Network.Managers
{
    /// <summary>
    /// GamePhase를 나누는 이유
    /// => 모든 플레이어의 시간을 하나로 맞추는 기준점
    /// - 동기화의 단순화 : 지금은 몇분 몇초고, 남은 시간은 얼마고.. 복잡한 데이터를 보낼 필요 없이 서버가 지금은 Ready 페이즈다 선포하면 모든 클라이언트는 약속된 UI를 띄우고 약속된 규칙을 이행하기 용이합니다.
    /// - 버그 방지 : 술래가 Ready 시간에 총을 쏠 수 있으면 안되므로, 코드 곳곳에 if 문을 떡칠하는 대신, CurrentPhase == GamePhase.Ready 일 때만 로직이 돌게하면 논리적 오류를 차단할 수 있습니다.
    /// - 확장성 : 나중에 새로운 규칙을 넣고 싶을 때, 새로운 페이즈 하나만 추가하면 기존 코드를 건드리지 않고 깔끔하게 삽입 가능합니다.
    /// </summary>
    public enum GamePhase { Lobby, Ready, Hide, Reroll, Final, Fever, Result}
    
    public class BirdGameManager : NetworkBehaviour
    {
        public static BirdGameManager Instance { get; private set; }

        public static event Action<int, int> OnPlayerCountChanged;
        public static event Action<string, string> OnPlayerKilled;
        
        public event Action<bool> OnSelectionPhaseStarted; // 매개변수: isSeeker (술래 여부)
        public event Action OnSelectionPhaseEnded;

        public event Action<bool, int> OnGameResult; // 매개변수: isSeekerWin, survivorCount
        public event Action OnGameResultEnded;
        
        [Networked] public TickTimer StateTimer { get; set; }
        [Networked] public GamePhase CurrentPhase { get; set; }
        [Networked] public PlayerRef Seeker { get; set; }
        [Networked] public NetworkBool IsSeekerWin { get; set; }
        [Networked] public int FinalSurvivorCount { get; set; }
        
        private ChangeDetector _changeDetector;
        
        // 상태 패턴 필드
        private Dictionary<GamePhase, IGameState> _states;
        private IGameState _currentState;
        
        private int _lastSeekerCount = -1;
        private int _lastHiderCount = -1;

        public override void Spawned()
        {
            Instance = this;
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // 상태 저장소 초기화
            _states = new Dictionary<GamePhase, IGameState>
            {
                { GamePhase.Lobby, new LobbyState() },
                { GamePhase.Ready, new ReadyState() },
                { GamePhase.Hide, new HideState() },
                { GamePhase.Reroll, new RerollState() },
                { GamePhase.Final, new FinalState() },
                { GamePhase.Fever, new FeverState() },
                { GamePhase.Result, new ResultState() }
            };

            // 초기 상태 설정
            TransitionToState(CurrentPhase);
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentPhase):
                        TransitionToState(CurrentPhase);
                        break;
                }
            }
        }

        private void TransitionToState(GamePhase nextPhase)
        {
            _currentState?.Exit(this);
            
            if (_states.TryGetValue(nextPhase, out var newState))
            {
                _currentState = newState;
                _currentState.Enter(this);
                Debug.Log($"[Bird] 페이즈 전환: {nextPhase}");
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 현재 상태의 로직 실행
            _currentState?.FixedUpdate(this);
        }

        public void SetPhase(GamePhase nextPhase)
        {
            if (!HasStateAuthority) return;
            CurrentPhase = nextPhase;
        }

        public void SetTimer(float duration)
        {
            if (!HasStateAuthority) return;
            StateTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        public void CheckGameOver()
        {
            if (!HasStateAuthority) return;

            int aliveHiders = 0;
            int aliveSeekers = 0;

            foreach (var player in Runner.ActivePlayers)
            {
                var playerObj = Runner.GetPlayerObject(player);
                if (playerObj == null) continue;

                var controller = playerObj.GetComponent<BirdPlayerController>();
                if (controller == null || controller.Health.CurrentHP <= 0) continue;

                if (player == Seeker) aliveSeekers++;
                else aliveHiders++;
            }
            
            if (_lastSeekerCount != aliveSeekers || _lastHiderCount != aliveHiders)
            {
                _lastSeekerCount = aliveSeekers;
                _lastHiderCount = aliveHiders;
                RPC_BroadcastPlayerCount(aliveSeekers, aliveHiders);
            }

            if (aliveSeekers <= 0) EndGame(false, aliveHiders);
            else if (aliveHiders <= 0) EndGame(true, 0);
        }

        public void EndGame(bool seekerWin, int survivorCount)
        {
            if (CurrentPhase == GamePhase.Result) return;
            
            IsSeekerWin = seekerWin;
            FinalSurvivorCount = survivorCount;
            SetPhase(GamePhase.Result);
        }

        public void NotifyPlayerKilled(PlayerRef attacker, PlayerRef victim)
        {
            if (!HasStateAuthority) return;
            string attackerName = attacker == PlayerRef.None ? "System" : $"Player {attacker.PlayerId}";
            string victimName = $"Player {victim.PlayerId}";
            RPC_BroadcastKillLog(attackerName, victimName);
        }

        public void ManualStartGame()
        {
            if (!HasStateAuthority || Runner.ActivePlayers.Count() < 2) return;

            // 랜덤 술래 정하기
            int randomIndex = Random.Range(0, Runner.ActivePlayers.Count());
            int i = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                if (i == randomIndex) Seeker = player;
                i++;
            }
            
            SetPhase(GamePhase.Ready);
            RPC_BroadcastPlayerCount(1, Runner.ActivePlayers.Count() - 1);
        }

        /// <summary>
        /// 플레이어 퇴장 시 호출되어 게임 지속 가능 여부를 판정합니다. (서버 전용)
        /// </summary>
        public void HandlePlayerLeft(PlayerRef leftPlayer)
        {
            if (!HasStateAuthority) return;

            // 로비나 이미 결과창인 경우는 무시합니다.
            if (CurrentPhase == GamePhase.Lobby || CurrentPhase == GamePhase.Result) return;

            int activeCount = Runner.ActivePlayers.Count();

            // 혼자 남은 경우 강제 종료
            if (activeCount <= 1)
            {
                Debug.Log("[Bird] 인원 부족으로 게임을 강제 종료합니다.");
                bool isRemainingSeeker = (Seeker != leftPlayer); 
                EndGame(isRemainingSeeker, activeCount);
                return;
            }

            // 술래가 나간 경우
            if (Seeker == leftPlayer)
            {
                Debug.Log("[Bird] 술래가 도주했습니다! 도망자의 승리입니다.");
                EndGame(false, activeCount);
                return;
            }

            // 도망자가 나간 경우 승패 다시 체크
            CheckGameOver();
        }
        
        public void TriggerSelectionPhase(bool isSeeker, bool isStart)
        {
            if (isStart) OnSelectionPhaseStarted?.Invoke(isSeeker);
            else OnSelectionPhaseEnded?.Invoke();
        }

        public void TriggerGameResult(bool isSeekerWin, int survivorCount, bool isStart)
        {
            if (isStart) OnGameResult?.Invoke(isSeekerWin, survivorCount);
            else OnGameResultEnded?.Invoke();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastPlayerCount(int seekers, int hiders) => OnPlayerCountChanged?.Invoke(seekers, hiders);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastKillLog(string attackerName, string victimName) => OnPlayerKilled?.Invoke(attackerName, victimName);
    }
}
