using UnityEngine;

namespace Bird.Network.Player
{
    public class TpsCameraStrategy : ICameraStrategy
    {
        public void OnEnter(Transform camera, Transform target) { }

        public void UpdateCamera(Transform camera, Transform target, CameraUpdateParams p)
        {
            // 회전된 오프셋 계산
            Quaternion rotation = Quaternion.Euler(p.Pitch, p.Yaw, 0);
            Vector3 rotatedOffset = rotation * p.Offset;
            
            // 타겟(플레이어) 위치 + 오프셋 적용
            camera.position = target.position + rotatedOffset;
            camera.rotation = rotation;
        }
    }
}
