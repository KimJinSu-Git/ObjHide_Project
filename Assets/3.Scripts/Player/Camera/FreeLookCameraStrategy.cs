using Bird.Network.UI;
using UnityEngine;

namespace Bird.Network.Player
{
    public class FreeLookCameraStrategy : ICameraStrategy
    {
        public void OnEnter(Transform camera, Transform target)
        {
            // 자유 시점 시작 시점의 위치를 기준점으로 설정
            CameraRotationHandler.SetInitialFreePos(camera.position);
        }

        public void UpdateCamera(Transform camera, Transform target, CameraUpdateParams p)
        {
            camera.rotation = Quaternion.Euler(p.Pitch, p.Yaw, 0);
            
            camera.position = CameraRotationHandler.GetFreeLookUpdate(camera, p.Input);
        }
    }
}
