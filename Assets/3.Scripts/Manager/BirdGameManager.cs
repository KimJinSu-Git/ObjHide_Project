using System;
using System.Linq;
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

        public override void Spawned()
        {
            Instance = this;
            Debug.Log("[Bird] 게임 매니저 네트워크 스폰 완료");
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

            // 타이머가 만료되었을 때 다음 단계로 진행
            if (StateTimer.Expired(Runner))
            {
                AdvancePhase();
            }
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
    }
}
