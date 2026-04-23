using UnityEngine;

namespace Bird.Network.Player
{
    public class FpsCameraStrategy : ICameraStrategy
    {
        public void OnEnter(Transform camera, Transform target) { }

        public void UpdateCamera(Transform camera, Transform target, CameraUpdateParams p)
        {
            if (p.Anchor == null) return;
            
            // 앵커 위치에 고정하고 회전 적용
            camera.position = p.Anchor.position;
            camera.rotation = Quaternion.Euler(p.Pitch, p.Yaw, 0);
        }
    }
}
