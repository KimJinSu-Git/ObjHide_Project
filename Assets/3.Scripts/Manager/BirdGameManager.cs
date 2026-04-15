using System;
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

        // 서버에서만 수정 가능한 네트워크 변수
        [Networked] public TickTimer StateTimer { get; set; }
        [Networked] public GamePhase CurrentPhase { get; set; }
        [Networked] public PlayerRef Seeker { get; set; }
        [Networked] public NetworkBool IsSeekerWin { get; set; }
        [Networked] public int FinalSurvivorCount { get; set; }
        
        private ChangeDetector _changeDetector;

        public override void Spawned()
        {
            Instance = this;
            
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            Debug.Log("[Bird] 게임 매니저 네트워크 스폰 완료");
            UpdateUIForPhase(CurrentPhase);
        }

        public override void Render()
        {
            // 3. 매 프레임 네트워크 변수의 변경을 감지 (이전 값과 달라졌을 때만 실행됨)
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentPhase):
                        UpdateUIForPhase(CurrentPhase);
                        break;
                }
            }
        }

        private void UpdateUIForPhase(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Result)
            {
                if (ResultUIHandler.Instance != null)
                {
                    ResultUIHandler.Instance.ShowResult(IsSeekerWin, FinalSurvivorCount);
                }
            }
            else
            {
                if (ResultUIHandler.Instance != null) ResultUIHandler.Instance.CloseUI();
                
                bool isSeeker = Runner.LocalPlayer == Seeker;

                if (!isSeeker && (newPhase == GamePhase.Ready || newPhase == GamePhase.Reroll))
                {
                    if (PropSelectionUIHandler.Instance != null)
                    {
                        PropSelectionUIHandler.Instance.hasSelected = false;
                        PropSelectionUIHandler.Instance.OpenSelectionUI();
                    }
                }
                else
                {
                    if (PropSelectionUIHandler.Instance != null)
                    {
                        PropSelectionUIHandler.Instance.CloseUI();
                    }
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 서버만 게임의 흐름을 통제할 권한이 있어야 합니다.
            if (!HasStateAuthority) return;

            if (CurrentPhase == GamePhase.Lobby)
            {
                if (Runner.ActivePlayers.Count() >= 2)
                {
                    StartGame();
                }
                return;
            }

            // 게임 진행 중일 때만 승패 판정 체크 (Ready 제외)
            if (CurrentPhase != GamePhase.Ready && CurrentPhase != GamePhase.Result)
            {
                CheckGameOver();
            }

            // 타이머가 만료되었을 때 다음 단계로 진행
            if (StateTimer.Expired(Runner))
            {
                if (CurrentPhase == GamePhase.Fever)
                {
                    // 시간 종료 시 현재 생존해 있는 도망자 수를 계산하여 전달
                    int survivors = Runner.ActivePlayers.Count(p => {
                        var obj = Runner.GetPlayerObject(p);
                        if (obj == null) return false;
                        var ctrl = obj.GetComponent<BirdPlayerController>();
                        return p != Seeker && ctrl != null && ctrl.CurrentHP > 0;
                    });
                    EndGame(false, survivors); 
                }
                else
                {
                    AdvancePhase();
                }
            }
        }

        private void CheckGameOver()
        {
            int aliveHiders = 0;
            bool isSeekerAlive = false;

            foreach (var player in Runner.ActivePlayers)
            {
                var playerObj = Runner.GetPlayerObject(player);
                if (playerObj == null) continue;

                var controller = playerObj.GetComponent<BirdPlayerController>();
                if (controller == null || controller.CurrentHP <= 0) continue;

                if (player == Seeker)
                {
                    isSeekerAlive = true;
                }
                else
                {
                    aliveHiders++;
                }
            }

            // 술래가 죽었거나 도망자가 전멸했을 때 게임 종료
            if (!isSeekerAlive)
            {
                EndGame(false, aliveHiders); // 도망자 승리
            }
            else if (aliveHiders <= 0)
            {
                EndGame(true, 0); // 술래 승리
            }
        }

        private void EndGame(bool seekerWin, int survivorCount)
        {
            if (CurrentPhase == GamePhase.Result) return;
            
            IsSeekerWin = seekerWin;
            FinalSurvivorCount = survivorCount;
            
            Debug.Log($"[Bird] 게임 종료! 승리팀: {(seekerWin ? "술래" : "도망자")}, 생존자: {survivorCount}");
            SetPhase(GamePhase.Result, 20f); // 20초 동안 결과 창 표시
        }

        private void StartGame()
        {
            // 랜덤 술래 정하기
            int randomIndex = Random.Range(0, Runner.ActivePlayers.Count());
            int i = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                if (i == randomIndex) Seeker = player;
                i++;
            }
            
            // 게임 시작
            SetPhase(GamePhase.Ready, 60f);
            Debug.Log($"[Bird] 게임 시작! 술래는 {Seeker}입니다.");
        }

        private void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case GamePhase.Ready: 
                    SetPhase(GamePhase.Hide, 120f); // 120초동안 1차 라운드 시작
                    break;
                case GamePhase.Hide:
                    SetPhase(GamePhase.Reroll, 20f); // 20초 동안 사물 리롤 시작
                    break;
                case GamePhase.Reroll:
                    SetPhase(GamePhase.Final, 70f); // 70초 동안 2차 라운드 시작
                    break;
                case GamePhase.Final:
                    SetPhase(GamePhase.Fever, 30f); // 피버타임 (30초동안 술래 피 소모 없음)
                    break;
                case GamePhase.Fever:
                    SetPhase(GamePhase.Result, 20f); // 20초 동안 결과 창 보여주기
                    break;
            }
        }

        private void SetPhase(GamePhase nextPhase, float duration)
        {
            CurrentPhase = nextPhase;
            StateTimer = TickTimer.CreateFromSeconds(Runner, duration);
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

            if (activeCount <= 1)
            {
                Debug.Log("[Bird] 인원 부족으로 게임을 강제 종료합니다.");
                // 혼자 남은 사람이 술래라면 술래 승, 도망자라면 도망자 승으로 처리
                bool isRemainingSeeker = (Seeker != leftPlayer); 
                EndGame(isRemainingSeeker, activeCount);
                return;
            }

            // 술래가 나간 경우 경우
            if (Seeker == leftPlayer)
            {
                Debug.Log("[Bird] 술래가 도주했습니다! 도망자의 승리입니다.");
                EndGame(false, activeCount); // 도망자 승리
                return;
            }

            // 도망자가 나간 경우
            CheckGameOver();
        }
    }
}
