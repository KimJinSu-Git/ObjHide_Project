using System.Linq;
using Bird.Network.Player;
using Bird.Network.UI;
using Fusion;
using UnityEngine;

namespace Bird.Network.Managers
{
    // --- Lobby State ---
    public class LobbyState : IGameState
    {
        public void Enter(BirdGameManager manager) { }
        public void FixedUpdate(BirdGameManager manager) { }
        public void Exit(BirdGameManager manager) { }
    }

    // --- Ready State ---
    public class ReadyState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            // 타이머 초기화
            if (manager.HasStateAuthority) manager.SetTimer(60f);

            // 도망자에게 사물 선택 UI 표시
            bool isSeeker = manager.Runner.LocalPlayer == manager.Seeker;
            manager.TriggerSelectionPhase(isSeeker, true);
        }

        public void FixedUpdate(BirdGameManager manager)
        {
            if (manager.HasStateAuthority && manager.StateTimer.Expired(manager.Runner))
            {
                manager.SetPhase(GamePhase.Hide);
            }
        }

        public void Exit(BirdGameManager manager)
        {
            manager.TriggerSelectionPhase(false, false);
        }
    }

    // --- Hide State (1차 라운드) ---
    public class HideState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            if (manager.HasStateAuthority) manager.SetTimer(120f);
        }

        public void FixedUpdate(BirdGameManager manager)
        {
            if (!manager.HasStateAuthority) return;

            manager.CheckGameOver();
            if (manager.StateTimer.Expired(manager.Runner))
            {
                manager.SetPhase(GamePhase.Reroll);
            }
        }

        public void Exit(BirdGameManager manager) { }
    }

    // --- Reroll State ---
    public class RerollState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            if (manager.HasStateAuthority) manager.SetTimer(20f);

            bool isSeeker = manager.Runner.LocalPlayer == manager.Seeker;
            manager.TriggerSelectionPhase(isSeeker, true);
        }

        public void FixedUpdate(BirdGameManager manager)
        {
            if (manager.HasStateAuthority && manager.StateTimer.Expired(manager.Runner))
            {
                manager.SetPhase(GamePhase.Final);
            }
        }

        public void Exit(BirdGameManager manager)
        {
            manager.TriggerSelectionPhase(false, false);
        }
    }

    // --- Final State (2차 라운드) ---
    public class FinalState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            if (manager.HasStateAuthority) manager.SetTimer(70f);
        }

        public void FixedUpdate(BirdGameManager manager)
        {
            if (!manager.HasStateAuthority) return;

            manager.CheckGameOver();
            if (manager.StateTimer.Expired(manager.Runner))
            {
                manager.SetPhase(GamePhase.Fever);
            }
        }

        public void Exit(BirdGameManager manager) { }
    }

    // --- Fever State ---
    public class FeverState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            if (manager.HasStateAuthority) manager.SetTimer(30f);
        }

        public void FixedUpdate(BirdGameManager manager)
        {
            if (!manager.HasStateAuthority) return;

            manager.CheckGameOver();
            if (manager.StateTimer.Expired(manager.Runner))
            {
                int survivors = 0;
                foreach (var kvp in manager.PlayerDict)
                {
                    if (kvp.Key != manager.Seeker && kvp.Value.Health.CurrentHP > 0)
                    {
                        survivors++;
                    }
                }
                manager.EndGame(false, survivors); 
            }
        }

        public void Exit(BirdGameManager manager) { }
    }

    // --- Result State ---
    public class ResultState : IGameState
    {
        public void Enter(BirdGameManager manager)
        {
            if (manager.HasStateAuthority) manager.SetTimer(20f);
            manager.TriggerGameResult(manager.IsSeekerWin, manager.FinalSurvivorCount, true);
        }

        public void FixedUpdate(BirdGameManager manager) { }

        public void Exit(BirdGameManager manager)
        {
            manager.TriggerGameResult(false, 0, false);
        }
    }
}
