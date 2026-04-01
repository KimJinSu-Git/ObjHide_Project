using UnityEngine;
using UnityEngine.EventSystems;

namespace Bird.Network.UI
{
    public class CameraRotationHandler : MonoBehaviour, IDragHandler
    {
        public static float CurrentYaw { get; private set; }
        public static float CurrentPitch { get; private set; }


        [SerializeField] private float sensitivity = 0.1f;

        // 카메라 상하 회전 제한
        [SerializeField] private float minPitch = -45f;
        [SerializeField] private float maxPitch = 45f;

        public void OnDrag(PointerEventData eventData)
        {
            // 좌우 회전
            CurrentYaw += eventData.delta.x * sensitivity;
            
            // 상하 회전 (마우스를 위로 올리면 (delta.y > 0) 카메라가 위를 봐야 하므로 빼줍니다.
            CurrentPitch -= eventData.delta.y * sensitivity;

            CurrentPitch = Mathf.Clamp(CurrentPitch, minPitch, maxPitch);
        }
    }
}