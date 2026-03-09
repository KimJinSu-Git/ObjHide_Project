using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class FireButtonHandler : MonoBehaviour
    {
        public static FireButtonHandler Instance { get; private set; }

        [SerializeField] private Button fireButton;

        private void Awake()
        {
            Instance = this;
            fireButton.gameObject.SetActive(false);
        }

        public void SetUpButton(Action onClickAction)
        {
            fireButton.gameObject.SetActive(true);
            fireButton.onClick.RemoveAllListeners();
            fireButton.onClick.AddListener(() => onClickAction?.Invoke());
        }

        public void SetVisible(bool visible)
        {
            fireButton.gameObject.SetActive(visible);
        }
    }
}
