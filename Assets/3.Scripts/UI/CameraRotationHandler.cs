using UnityEngine;
using UnityEngine.EventSystems;

namespace Bird.Network.UI
{
    public class CameraRotationHandler : MonoBehaviour, IDragHandler
    {
        public static float CurrentYaw { get; private set; }
        [SerializeField] private float sensitivity = 0.1f;

        public void OnDrag(PointerEventData eventData)
        {
            // 드래그한 거리만큼 각도(Yaw) 누적
            CurrentYaw += eventData.delta.x * sensitivity;
        }
    }
}