namespace Bird.Network.Managers
{
    public interface IGameState
    {
        /// <summary>
        /// 해당 페이즈에 진입할 때 1번 실행 (UI 초기화, 타이머 설정 등)
        /// </summary>
        void Enter(BirdGameManager manager);

        /// <summary>
        /// 매 네트워크 틱(FixedUpdateNetwork)마다 실행 (승패 체크, 시간 체크 등)
        /// </summary>
        void FixedUpdate(BirdGameManager manager);

        /// <summary>
        /// 페이즈를 벗어날 때 실행 (정리 작업)
        /// </summary>
        void Exit(BirdGameManager manager);
    }
}
