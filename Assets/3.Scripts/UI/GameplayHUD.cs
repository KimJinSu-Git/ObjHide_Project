using UnityEngine;

namespace Bird.Network.UI
{
    /// <summary>
    /// 게임씬 필드에 미리 배치되어, 동적으로 생성되는 플레이어들에게 UI 포인터를 제공합니다
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public static GameplayHUD Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject crosshairUI;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 플레이어 역할에 따라 조준선을 설정합니다
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            if (crosshairUI != null)
            {
                crosshairUI.SetActive(visible);
            }
        }
    }
}