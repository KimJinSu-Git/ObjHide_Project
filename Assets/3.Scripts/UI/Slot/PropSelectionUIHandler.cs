using System.Collections;
using Bird.Network.Data;
using Bird.Network.Player;
using TMPro;
using UnityEngine;

namespace Bird.Network.UI
{
    public class PropSelectionUIHandler : MonoBehaviour
    {
        public static PropSelectionUIHandler Instance { get; private set; }
        
        [SerializeField] private GameObject panel; // 슬롯머신 전체 패널
        [SerializeField] private BirdPropSlotUI[] slots; // 3개의 슬롯
        [SerializeField] private PropDatabase propDatabase;
        [SerializeField] private TextMeshProUGUI timerText;

        private Coroutine timerCoroutine;
        
        public bool hasSelected = false;

        private void Awake()
        {
            Instance = this;
        }

        public void OpenSelectionUI()
        {
            if (hasSelected) return;
            
            panel.SetActive(true);
            hasSelected = false;

            foreach (var slot in slots)
            {
                slot.SetupSlot(propDatabase.GetRandomProp(), (id) => ConfirmSelection(id));
                
                if(timerCoroutine != null) StopCoroutine(timerCoroutine);
                timerCoroutine = StartCoroutine(Co_StartTimer(20f));
                
                slot.SetRerollActive(true); // 처음 한번은 리롤 가능
            }
        }

        private IEnumerator Co_StartTimer(float duration)
        {
            float remaining = duration;
            while (remaining > 0)
            {
                timerText.text = $"Choose Time : {Mathf.CeilToInt(remaining)}s)";
                yield return new WaitForSeconds(1f);
                remaining--;
            }

            if (!hasSelected)
            {
                int randomSlotIndex = Random.Range(0, slots.Length);
                ConfirmSelection(slots[randomSlotIndex].CurrentPropID);
            }
        }

        private void ConfirmSelection(int propID)
        {
            hasSelected = true;
            // 내 캐릭터 컨트롤러를 찾아 RPC 호출
            var myPlayer = BirdPlayerController.Local;
            if (myPlayer != null)
            {
                myPlayer.RPC_RequestChangeProp(propID);
            }
            CloseUI();
        }

        public void CloseUI()
        {
            panel.SetActive(false);
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        }
    }

}
