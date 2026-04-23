using UnityEngine;

namespace Bird.Network.Player
{
    /// <summary>
    /// 카메라 시점 계산에 필요한 데이터 묶음
    /// </summary>
    public struct CameraUpdateParams
    {
        public float Pitch;
        public float Yaw;
        public Vector3 Input; // FreeLook 이동용 입력
        public Vector3 Offset; // TPS용 오프셋
        public Transform Anchor; // FPS용 앵커
    }

    public interface ICameraStrategy
    {
        /// <summary>
        /// 카메라의 위치와 회전을 계산하여 적용
        /// </summary>
        void UpdateCamera(Transform cameraTransform, Transform targetTransform, CameraUpdateParams p);
        
        /// <summary>
        /// 시점 전환 시 초기화가 필요한 경우 사용
        /// </summary>
        void OnEnter(Transform cameraTransform, Transform targetTransform);
    }
}
