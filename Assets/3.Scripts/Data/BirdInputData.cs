using Fusion;
using UnityEngine;

namespace Bird.Network.Data
{
    public struct BirdInputData : INetworkInput
    {
        public Vector3 Movement; // 조이스틱 입력 값
        public float LookYaw; // 카메라 회전 수평 각도
        // TODO :: 사물변신 버튼이나, 점프 버튼 등도 아래에 추가될 예정입니다.
    }

}
