using Fusion;
using UnityEngine;

namespace Bird.Network.Data
{
    public enum PlayerInputButtons
    {
        Jump = 0
    }
    
    public struct BirdInputData : INetworkInput
    {
        public Vector3 Movement; // 조이스틱 입력 값
        public float LookYaw; // 카메라 회전 수평 각도
        public float LookPitch;
        public NetworkButtons Buttons; // 점프 등의 버튼 입력을 담을 그릇
    }
}
