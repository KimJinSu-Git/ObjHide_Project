using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    /// <summary>
    /// 도망자(Hider)의 정렬/잠금 토글 및 각도 조절 서브 버튼을 관리합니다.
    /// </summary>
    public class PropAlignmentHandler : MonoBehaviour
    {
        public static PropAlignmentHandler Instance { get; private set; }

        [Header("Main Button")]
        [SerializeField] private Button alignToggleButton; // 정렬 및 잠금 토글 버튼

        [Header("Sub Buttons")]
        [SerializeField] private GameObject subButtonsContainer;
        [SerializeField] private Button rotate45Button;
        [SerializeField] private Button rotate90Button;

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

            SetVisible(false);
            SetSubButtonsVisible(false);
        }

        /// <summary>
        /// 로컬 플레이어가 스폰되었을 때 버튼 이벤트를 연결합니다.
        /// </summary>
        public void SetUpButtons(Action onToggleLock, Action onRotate45, Action onRotate90)
        {
            alignToggleButton.onClick.RemoveAllListeners();
            alignToggleButton.onClick.AddListener(() => onToggleLock?.Invoke());

            rotate45Button.onClick.RemoveAllListeners();
            rotate45Button.onClick.AddListener(() => onRotate45?.Invoke());

            rotate90Button.onClick.RemoveAllListeners();
            rotate90Button.onClick.AddListener(() => onRotate90?.Invoke());
        }

        /// <summary>
        /// 도망자일 때만 메인 정렬 버튼을 활성화합니다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            alignToggleButton.gameObject.SetActive(visible);
            if (!visible) SetSubButtonsVisible(false); // 전체가 꺼지면 서브 버튼도 강제 종료
        }

        /// <summary>
        /// 잠금(Lock) 상태에 따라 서브 버튼 컨테이너를 켜고 끕니다.
        /// </summary>
        public void SetSubButtonsVisible(bool visible)
        {
            if (subButtonsContainer != null)
            {
                subButtonsContainer.SetActive(visible);
            }
        }
    }
}