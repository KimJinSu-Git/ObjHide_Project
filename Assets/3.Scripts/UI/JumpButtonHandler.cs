using UnityEngine;
using UnityEngine.EventSystems;

namespace Bird.Network.UI
{
    /// <summary>
    /// 모바일 점프 버튼 UI에 부착되어 터치 시 입력을 전달합니다
    /// </summary>
    public class JumpButtonHandler : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            BirdInputManager.IsJumpPressed = true;
            Debug.Log("[UI] 모바일 점프 버튼 터치됨!");
        }
    }
}