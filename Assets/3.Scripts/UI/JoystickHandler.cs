using UnityEngine;
using UnityEngine.EventSystems;
using Bird.Network.UI;

namespace Bird.Network.UI
{
    public class JoystickHandler : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform handle;
        private RectTransform background;
        private float range = 100f;

        private void Awake()
        {
            background = GetComponent<RectTransform>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 pos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos))
            {
                pos = Vector2.ClampMagnitude(pos, range);
                handle.anchoredPosition = pos;
                
                // 입력 브릿지에 전달 (X, Z 평면 이동이므로 Y 대신 Z에 대입)
                BirdInputManager.Movement = new Vector3(pos.x, 0, pos.y).normalized;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            handle.anchoredPosition = Vector2.zero;
            BirdInputManager.Movement = Vector3.zero;
        }
    }
}