using Bird.Network.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bird.Network.UI
{
    public class BirdPropSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI propNameText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button rerollButton;

        private BirdPropData currentData;

        public int CurrentPropID => currentData != null ? currentData.PropID : -1;

        // 슬롯 데이터 채우기
        public void SetupSlot(BirdPropData data, System.Action<int> onSelect)
        {
            currentData = data;
            propNameText.text = data.PropName;
            
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(currentData.PropID));
        }
        
        public void SetRerollActive(bool active) => rerollButton.interactable = active;
    }

}
